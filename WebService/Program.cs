using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Channel<DateTime>>((service) =>
{
    return Channel.CreateBounded<DateTime>(new BoundedChannelOptions(10) { FullMode = BoundedChannelFullMode.Wait });
});
builder.Services.AddSingleton<Random>((service) =>
{
    return new Random(); ;
});
// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/Read", async (Channel<DateTime> qe, Random rnd, CancellationToken ct) =>
{
    var timeout = new CancellationTokenSource(new TimeSpan(0, 0, 10));
    try
    {
        await qe.Writer.WriteAsync(DateTime.UtcNow.Add(new TimeSpan(0, 2, 0)), timeout.Token);
        //var number = rnd.Next(10);
        //if (number <= 3)
        //    throw new InvalidOperationException("Unspecified API Error");
        await Task.Delay(4000, ct);
        await qe.Reader.ReadAsync(ct);
    } catch (OperationCanceledException ex) when (timeout.Token.IsCancellationRequested){
        return Results.Problem();
    } catch (OperationCanceledException ex) when (ct.IsCancellationRequested) {
        return Results.NoContent();
    } catch (InvalidOperationException ex)
    {
        return Results.Problem();
    }
    catch (Exception ex)
    { 
        return Results.Problem();
    }
    return Results.Ok();
});

app.MapGet("/Expire", async (Channel<DateTime> qe, CancellationToken ct) =>
{
    while(qe.Reader.TryPeek(out DateTime dt) && dt  < DateTime.UtcNow)
        await qe.Reader.ReadAsync(ct);

    return;
});
app.MapGet("/Clear", async (Channel<DateTime> qe, CancellationToken ct) =>
{
    return qe.Reader.ReadAllAsync(ct);
});
app.Run();
