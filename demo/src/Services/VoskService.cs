using System;
using System.Threading.Tasks;
using Vosk;
using VoskDemo.Core.Interfaces;

namespace VoskDemo.Services
{
    public class VoskService : IVoskService
    {
        private Model? _model;
        private readonly object _lock = new();

        public async Task<Model> GetModelAsync(string modelPath, bool verbose = true)
        {
            if (_model == null)
            {
                await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        if (_model == null)
                        {
                            if (verbose) Console.WriteLine("Загрузка модели...");
                            _model = new Model(modelPath);
                        }
                    }
                });
            }
            return _model!;
        }

        public void Cleanup()
        {
            _model?.Dispose();
            _model = null;
        }
    }
} 