﻿
using CNABSolution.Server.DatabaseConfig.Database;
using CNABSolution.Server.Models.Transaction;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace CNABSolution.Server.Controller.CNABFileController
{
    // Classe usada para assumir a responsabilidade de controlador.
    public class CNABFileController
    {
        public static string local = "[CNABFILE-CONTROLLER]";
        public static IMongoCollection<Transaction> transactionCollection = Database.Client.GetDatabase("desafio_net").GetCollection<Transaction>("Transactions");
        private IFormFile cnabFile;
        public CNABFileController(IFormFile file)
        {
            this.cnabFile = file;
        }

        // Método responsável por tratar os dados contidos no arquivo padrão cnab
        public async Task<List<Transaction>> TreatCnabFile()
        {
            try
            {
                string fileContent = await ReadFileContent(cnabFile);
                string[] lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                List<Transaction> allTransactions = new List<Transaction>();

                foreach (var line in lines)
                {
                    List<Transaction> transactions = ParseCnabFile(line);
                    allTransactions.AddRange(transactions);
                }
                await RegisterTransaction(allTransactions);
                return allTransactions;
            } catch (Exception error)
            {
                Console.Error.WriteLine($"{local} - Failed trying to Configure file content: {error}");
                throw;
            }
        }

        // Método privado utilizado somente pela classe para ler o conteúdo do arquivo antes de tratar o mesmo.
        private async Task<string> ReadFileContent(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        // Método privado responsável por parsear/tratar os dados de transação contidos em cada linha do arquivo. 
        private List<Transaction> ParseCnabFile(string line)
        {
            try
            {
                List<Transaction> transactions = new List<Transaction>();
                string tipoTransacao = line.Substring(0, 1);
                string dataOcorrencia = line.Substring(1, 8);
                string valor = line.Substring(9, 10);
                string cpf = line.Substring(19, 11);
                string cartao = line.Substring(30, 12);
                string donoLoja = line.Substring(42, 14);
                string nomeLoja = line.Substring(56, 18);

                Transaction data = new Transaction
                {
                    type = tipoTransacao,
                    transaction_date = dataOcorrencia,
                    amount = valor,
                    cpf = cpf,
                    card_number = cartao,
                    store_owner = donoLoja,
                    store_name = nomeLoja,
                };

                transactions.Add(data);

                return transactions;

            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"{local} - Failed trying to parse cnab file: {error}");
                throw;
            }
        }

        // Método privado responsável por registar uma lista de transação(funciona com uma ou várias linhas)
        private async Task RegisterTransaction(List<Transaction> transactions)
        {
            try
            {
                await transactionCollection.InsertManyAsync(transactions);
            } catch (Exception error)
            {
                Console.Error.WriteLine(error);
                throw;
            }
        }

        // Método responsável por retornar uma lista com todas as transações cadastradas na base de dados
        // Usado na rota de transactions para renderizar os dados na tabela.
        // seu nivel de acesso é public e estático para que seja possivel ser acessado sem a necessidade de criar uma instancia.
        public static async Task<List<Transaction>> GetTransactions()
        {
            try
            {
                IAsyncCursor<Transaction> cursor = await transactionCollection.FindAsync(new BsonDocument());
                List<Transaction> transactions = await cursor.ToListAsync();
                return transactions;
            } catch (Exception error)
            {
                Console.Error.WriteLine($"{local} - Failed trying to get all transactions: {error}");
                throw;
            }
        }

    }
}
