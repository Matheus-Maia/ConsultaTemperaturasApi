using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace ConsultaTemperaturasApi.Controllers
{
    /// <summary>
    /// Controlador para consultar a viabilidade de plantação com base nas temperaturas registradas.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ConsultaController : ControllerBase
    {
        /// <summary>
        /// Caminho da pasta onde estão os bancos de dados SQLite.
        /// </summary>
        private readonly string _dbFolder;

        /// <summary>
        /// Inicializa uma nova instância do controlador de consulta de temperaturas.
        /// </summary>
        /// <param name="configuration">Configuração da aplicação que contém o caminho para o banco de dados.</param>
        public ConsultaController(IConfiguration configuration)
        {
            var dbFolder = configuration["DatabaseSettings:DatabaseFolder"];
            if (string.IsNullOrEmpty(dbFolder))
            {
                throw new InvalidOperationException("O caminho para o banco de dados não está configurado corretamente.");
            }
            _dbFolder = Path.Combine(Directory.GetCurrentDirectory(), dbFolder);

            if (!Directory.Exists(_dbFolder))
            {
                Directory.CreateDirectory(_dbFolder);
            }
        }

        /// <summary>
        /// Consulta a viabilidade de plantação para uma cultura específica.
        /// </summary>
        /// <param name="cidade">Nome da cidade onde a plantação será realizada.</param>
        /// <param name="ano">Ano de referência para a consulta das temperaturas (2012).</param>
        /// <param name="data">Data inicial da consulta no formato AAAA-MM-DD.</param>
        /// <param name="cultura">Cultura agrícola a ser consultada (ex.: arroz, milho).</param>
        /// <returns>Resultado da consulta.</returns>
        [HttpGet]
        [Produces("application/json")]
        [Consumes("application/json")]
        [SwaggerOperation(
            Summary = "Consulta a viabilidade de plantação para uma cultura específica.",
            Description = "Este endpoint verifica se as temperaturas são adequadas para plantar no local com a cultura escolhida."
        )]
        public IActionResult Get(
            [FromQuery] string cidade,
            [FromQuery] string ano,
            [FromQuery] string data,
            [FromQuery] string cultura
        )
        {
            // Validação dos parâmetros
            if (string.IsNullOrEmpty(cidade) || string.IsNullOrEmpty(ano) || string.IsNullOrEmpty(data) || string.IsNullOrEmpty(cultura))
            {
                return BadRequest("Os parâmetros 'cidade', 'ano', 'data' e 'cultura' são obrigatórios.");
            }

            // Monta o caminho do banco de dados 
            string dbPath = System.IO.Path.Combine(_dbFolder, $"{cidade}_{ano}.db");

            // Verifica se o banco de dados existe
            if (!System.IO.File.Exists(dbPath))
            {
                return NotFound("Banco de dados não encontrado para a cidade e ano informados.");
            }

            // Validação da data
            if (!DateTime.TryParseExact(data, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime dataConsulta))
            {
                return BadRequest("Data inválida. Use o formato AAAA-MM-DD.");
            }

            string dataStr = dataConsulta.ToString("yyyy-MM-dd");

            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();

                    // Verifica se a data inicial existe no banco de dados
                    string sqlVerificaData = @"
                        SELECT COUNT(*) 
                        FROM temperaturas 
                        WHERE [DATA (YYYY-MM-DD)] = @data";

                    using (var command = new SqliteCommand(sqlVerificaData, connection))
                    {
                        command.Parameters.AddWithValue("@data", dataStr);
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        if (count == 0)
                        {
                            return NotFound("A data informada não existe no banco de dados.");
                        }
                    }

                    // Define as fases do cultivo do arroz e suas durações possíveis
                    var fasesArroz = new[]
                    {
                        new { Nome = "Germinação", DuracaoMin = 5, DuracaoMax = 10 },
                        new { Nome = "Perfilhamento", DuracaoMin = 20, DuracaoMax = 40 },
                        new { Nome = "Alongamento", DuracaoMin = 10, DuracaoMax = 15 },
                        new { Nome = "Floração", DuracaoMin = 7, DuracaoMax = 10 },
                        new { Nome = "Maturação", DuracaoMin = 15, DuracaoMax = 20 }
                    };

                    // Calcular o número total de dias necessários
                    int totalDiasNecessarios = 0;
                    foreach (var fase in fasesArroz)
                    {
                        totalDiasNecessarios += fase.DuracaoMax;
                    }

                    // Verificar a quantidade de dias disponíveis no banco de dados
                    DateTime dataFinal = dataConsulta.AddDays(totalDiasNecessarios);
                    string sqlVerificaDiasDisponiveis = @"
                        SELECT COUNT(*) 
                        FROM temperaturas 
                        WHERE [DATA (YYYY-MM-DD)] BETWEEN @dataInicio AND @dataFinal";

                    using (var command = new SqliteCommand(sqlVerificaDiasDisponiveis, connection))
                    {
                        command.Parameters.AddWithValue("@dataInicio", dataStr);
                        command.Parameters.AddWithValue("@dataFinal", dataFinal.ToString("yyyy-MM-dd"));

                        int diasDisponiveis = Convert.ToInt32(command.ExecuteScalar());

                        if (diasDisponiveis < totalDiasNecessarios)
                        {
                            return BadRequest("O período de consulta é insuficiente para cobrir todas as fases do cultivo.");
                        }
                    }

                    DateTime dataAtual = dataConsulta;

                    // Itera sobre cada fase do cultivo
                    foreach (var fase in fasesArroz)
                    {
                        for (int diasFase = fase.DuracaoMin; diasFase <= fase.DuracaoMax; diasFase++)
                        {
                            string sqlConsulta = @"
                                SELECT temp_min, temp_max 
                                FROM temperaturas 
                                WHERE [DATA (YYYY-MM-DD)] = DATE(@data, '+' || @diasOffset || ' day')";

                            using (var command = new SqliteCommand(sqlConsulta, connection))
                            {
                                command.Parameters.AddWithValue("@data", dataStr);
                                command.Parameters.AddWithValue("@diasOffset", (dataAtual - dataConsulta).Days + diasFase);

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        double tempMin = LerTemperatura(reader, coluna: 0);
                                        double tempMax = LerTemperatura(reader, coluna: 1);

                                        // Verifica se as temperaturas são inválidas
                                        if (tempMin == 0 || tempMax == 0)
                                        {
                                            DateTime dataFalha = dataAtual.AddDays(diasFase);
                                            return Ok(new 
                                            { 
                                                Mensagem = $"Na data: {dataFalha:yyyy-MM-dd}, temperatura inválida encontrada no banco de dados.", 
                                                Cidade = cidade
                                            });
                                        }

                                        // Valida as temperaturas para a fase atual
                                        if (!ValidarTemperaturasParaFase(tempMin, tempMax, fase.Nome, cultura))
                                        {
                                            DateTime dataFalha = dataAtual.AddDays(diasFase);
                                            return Ok(new 
                                            { 
                                                Mensagem = $"A {cultura} se torna inviável na data: {dataFalha:yyyy-MM-dd}. Fase: {fase.Nome}", 
                                                Cidade = cidade
                                            });
                                        }
                                    }
                                }
                            }

                            dataAtual = dataAtual.AddDays(1); // Avança dia a dia
                        }
                    }
                }

                return Ok(new { Mensagem = "Consulta concluída. Plantação viável." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao consultar o banco de dados: {ex.Message}");
            }
        }

        // Função para ler temperaturas, tratando valores NULL
        private double LerTemperatura(SqliteDataReader reader, int coluna)
        {
            if (!reader.IsDBNull(coluna))
            {
                return reader.GetDouble(coluna);
            }
            return 0; // Valor padrão para NULL
        }

        // Função para validar as temperaturas com base na fase e na cultura
        private bool ValidarTemperaturasParaFase(double tempMin, double tempMax, string fase, string cultura)
        {
            var (min, max) = ObterFaixaTemperatura(fase, cultura);

            if (tempMin < min || tempMax > max)
            {
                Console.WriteLine($"{fase}, falha para {cultura}");
                return false;
            }
            return true;
        }

        // Função para obter a faixa de temperatura com base na fase e na cultura
        private (double Min, double Max) ObterFaixaTemperatura(string fase, string cultura)
        {
            switch (cultura.ToLower())
            {
                case "arroz":
                    switch (fase)
                    {
                        case "Germinação": return (10, 45);
                        case "Perfilhamento": return (9, 40);
                        case "Alongamento": return (7, 37);
                        case "Floração": return (17, 35);
                        case "Maturação": return (12, 30);
                        default: return (0, 0);
                    }

                default:
                    throw new ArgumentException("Cultura não suportada.");
            }
        }
    }
}
