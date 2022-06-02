// See https://aka.ms/new-console-template for more information
using Core;

Console.WriteLine("Click to Begin");
Console.Read();

string memoryFile = "test1";
var breaker = new CircuitBreaker(new MemCircuitStateSynchronization(memoryFile));
using var client = new HttpClient();

bool IsFaulted = false;
int concurrentRequests = 5;
List<Task> runningTasks = new List<Task>(10);

do
{
    //Lets make some concurrent calls!
    try { 
        for(var i = 0; i < concurrentRequests; i++) { 
            runningTasks.Add(
                    Task.Run(async () =>
                    {
                       await breaker.ExecuteAsync(async () =>
                       {
                           var response = await client.GetAsync("https://localhost:7232/read");
                           response.EnsureSuccessStatusCode();
                           Console.WriteLine("Sent Request!");
                           await Task.Delay(2500);
                       },
                       () =>
                       {
                           Console.WriteLine("Somethign wen't wrong. Aborting.");
                           return Task.CompletedTask;
                       }
                       );
                    })
                );
            }

    await Task.WhenAll(runningTasks.ToArray());
    } catch (Exception ex)
    {
        Console.WriteLine("Circuit Breaker Open: " + ex.Message);
        IsFaulted = true;
    }
} while (!IsFaulted);

//Circuit has faulted. Wait to retry:
int backoff = 250;
while (true)
{
    try
    {
        await breaker.ExecuteAsync(async () =>
        {
            var response = await client.GetAsync("https://localhost:7232/read");
            response.EnsureSuccessStatusCode();
            Console.WriteLine("Sent Request!");
            await Task.Delay(2500);
        });
    }
    catch (Exception ex)
    {
        backoff *= 2;
    }
    //keep trying at longer intervals until circuit has cleared
    await Task.Delay(backoff);
}