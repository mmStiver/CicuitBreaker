using Core.Contract;
using ProtoBuf;
using System.IO.MemoryMappedFiles;


namespace Core
{
    public class MemCircuitStateSynchronization : ICircuitStateSynchronization
    {
        private string _circuitName;

        public MemCircuitStateSynchronization(string circuitName)
        {
            _circuitName = circuitName;
        }

        public void Initialize(CircuitState status, CancellationToken ct)
        {
            using (var mmf = MemoryMappedFile.CreateNew("test1", 500,
                MemoryMappedFileAccess.ReadWriteExecute,
                MemoryMappedFileOptions.None,
                HandleInheritability.Inheritable))
            {

                Mutex mutex = new Mutex(true, "circuitstatemutex", out bool mutexCreated);
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    stream.Seek(sizeof(Int16), SeekOrigin.Begin);

                    Serializer.Serialize(stream, status);
                    long size = stream.Position - sizeof(Int16);

                    stream.Seek(0, SeekOrigin.Begin);
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(size);
                    mutex.ReleaseMutex();

                    while (!ct.IsCancellationRequested) ;
                }
            }
        }

        public TResult Get<TResult>(Func<CircuitState, TResult> func)
        {
                Mutex mutex = Mutex.OpenExisting("circuitstatemutex");
                mutex.WaitOne();
            using (var mmf = MemoryMappedFile.OpenExisting(this._circuitName, MemoryMappedFileRights.FullControl, HandleInheritability.Inheritable))
            {
                try
                {
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {

                        if (stream.CanRead)
                        {
                            long length;
                            BinaryReader reader = new BinaryReader(stream);
                            length = reader.ReadInt16();
                            
                            byte[] buffer = new byte[length];
                            stream.Read(buffer, 0, buffer.Length);

                            stream.Seek(0, SeekOrigin.Begin);

                            var status = Serializer.Deserialize<CircuitState>(buffer.AsSpan());
                            return func.Invoke(status);
                        }
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            throw new Exception();
        }
        public void Write<TResult>(Action<CircuitState> action)
        {
                Mutex mutex = Mutex.OpenExisting("circuitstatemutex");
                mutex.WaitOne();
            using (var mmf = MemoryMappedFile.OpenExisting(this._circuitName, MemoryMappedFileRights.FullControl, HandleInheritability.Inheritable))
            {
                try
                {
                    CircuitState status;
                    using (MemoryMappedViewAccessor stream = mmf.CreateViewAccessor())
                    {
                        long length = stream.ReadInt16(0);

                        byte[] buffer = new byte[length];
                        stream.ReadArray<byte>(sizeof(Int16), buffer, 0, buffer.Length);

                        status = Serializer.Deserialize<CircuitState>(buffer.AsSpan());
                    }
                    var original = status.Clone();
                    action.Invoke(status);
                    if (status != original)
                    {
                        using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                        {
                            stream.Seek(sizeof(Int16), SeekOrigin.Begin);

                            Serializer.Serialize(stream, status);
                            Int16 size = (Int16)((stream.Position) - sizeof(Int16));

                            stream.Seek(0, SeekOrigin.Begin);
                            BinaryWriter writer = new BinaryWriter(stream);
                            writer.Write(size);
                        }
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
        public TResult Write<TResult>(Func<CircuitState, TResult> func)
        {
            using (var mmf = MemoryMappedFile.OpenExisting(this._circuitName, MemoryMappedFileRights.FullControl, HandleInheritability.Inheritable))
            {
                Mutex mutex = Mutex.OpenExisting("circuitstatemutex");
                mutex.WaitOne();
                try
                {
                    CircuitState status;
                    using (MemoryMappedViewAccessor stream = mmf.CreateViewAccessor())
                    {
                        long length = stream.ReadInt16(0);

                        byte[] buffer = new byte[length];
                        stream.ReadArray<byte>(sizeof(Int16), buffer, 0, buffer.Length);

                        status = Serializer.Deserialize<CircuitState>(buffer.AsSpan());
                    }
                    var original = status.Clone();
                    var result = func.Invoke(status);
                    if (status != original)
                    {
                        using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                        {
                            stream.Seek(sizeof(Int16), SeekOrigin.Begin);

                            Serializer.Serialize(stream, status);
                            Int16 size = (Int16)((stream.Position) - sizeof(Int16));

                            stream.Seek(0, SeekOrigin.Begin);
                            BinaryWriter writer = new BinaryWriter(stream);
                            writer.Write(size);
                        }
                    }
                    return result;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}