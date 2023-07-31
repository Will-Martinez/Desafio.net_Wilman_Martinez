﻿using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Extensions;
using CNABSolution.Server.Models.Transaction;
using CNABSolution.Server.Controller.CNABFileController;

namespace CNABSolution.RoutesCNAB;

// Classe responsável por criar as API's de envio de arquivo padrão cnab e retorno das transações cadastradas
public class CNABFileAPI
{
    public static string local = "[CNAB-ROUTES]";

    // Método publico e estático para que a classe de configuração acesse sem necessidade de criar uma instancia
    // do tipo CNABFileAPI
    public static void MapRoutes(IEndpointRouteBuilder endpoint)
    {
        // endpoint para envio do arquivo de padrão cnab. aqui é feito uma instancia do tipo CNABFileController
        // para que seja feito os restantes dos passos como parse, leitura do arquivo e etc
        endpoint.MapPost("/api/sendFile", async context =>
        {
            try
            {
                var file = context.Request.Form.Files["arquivo"];
                if(file == null)
                {
                    context.Response.StatusCode = 400;
                    Console.WriteLine($"{local} - Failed trying to send cnab file.");
                    throw new Exception("CNAB file is required to transations saving.");
                }
                var transactionsController = new CNABFileController(file);
                List<Transaction>allTransactions = await transactionsController.TreatCnabFile();
                if (allTransactions == null)
                {
                    context.Response.StatusCode = 404;
                    Console.Error.WriteLine($"{local} - Transactions not founded.");
                    throw new Exception($"{local} - Transactions not founded.");
                }
                context.Response.StatusCode = 200;
                await context.Response.WriteAsJsonAsync(allTransactions);
            } catch (Exception error)
            {
                context.Response.StatusCode = 500;
                Console.Error.Write($"{local} - Internal server error: {error}");
                throw;
            }
        });

        // endpoint responsável por retornar todas as transações feitas até o momento. Dados são buscados na collection
        // Transactions
        endpoint.MapGet("/api/getTransactions", async context =>
        {
            try
            {
                List<Transaction> transactions = await CNABFileController.GetTransactions();
                if(transactions == null)
                {
                    context.Response.StatusCode = 404;
                    Console.Error.WriteLine($"{local} - Transactions not founded.");
                    throw new Exception("Transactions not founded");
                }
                context.Response.StatusCode = 200;
                await context.Response.WriteAsJsonAsync(transactions);
            } catch (Exception error)
            {
                context.Response.StatusCode = 500;
                Console.Error.WriteLine($"{local} - Internal server error: {error}");
                throw;
            }
        });
    }
}
