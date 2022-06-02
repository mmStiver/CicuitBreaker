// See https://aka.ms/new-console-template for more information
using Core;
using ProtoBuf;
using System.IO.MemoryMappedFiles;

Console.WriteLine("Initializing");
string mmap = string.Empty;
if (args.Length > 0)
{
     mmap = args[0] ?? "test1";
} else
{
    mmap =  "test1";
}

var state = new MemCircuitStateSynchronization(mmap);
int maxFailures = 5;
TimeSpan faultTimeout = new(0,0,15);
int successCount = 5;

//Create and manage memory mapped file for synchronization
var memory = Task.Run(() => {
    using (var mmf = MemoryMappedFile.CreateNew(mmap, 500,
        MemoryMappedFileAccess.ReadWriteExecute,
        MemoryMappedFileOptions.None,
        HandleInheritability.Inheritable))
    {

        Mutex mutex = new Mutex(true, "circuitstatemutex", out bool mutexCreated);
        using (MemoryMappedViewStream stream = mmf.CreateViewStream())
        {
            var status = new CircuitState(CircuitStateKind.Closed, 0, 0,0, maxFailures, faultTimeout.Ticks, successCount, faultTimeout.Ticks);
            stream.Seek(sizeof(Int16), SeekOrigin.Begin);

            Serializer.Serialize(stream, status);
            Int16 size = (Int16)((stream.Position) - sizeof(Int16));

            stream.Seek(0, SeekOrigin.Begin);
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(size);
            mutex.ReleaseMutex();
            while (true)
            {
                //Periodically check the status of our circuit breaker
                state.Write<bool>((CircuitState status) =>
                {
                    if (status.State == CircuitStateKind.Open && status.FaultTimeout < DateTime.UtcNow.Ticks)
                    {
                        status.State = CircuitStateKind.HalfOpen;
                        status.Success= 0;
                    }
                    else if (status.State == CircuitStateKind.HalfOpen && (status.Success >= status.SuccessThreshold ))
                    {
                        status.State = CircuitStateKind.Closed;
                        status.Failures = 0;
                    }
                    return true;
                });
                Task.Delay(1000);
            }
        }
        Console.WriteLine("Done!");

    }
});

Console.Read();
