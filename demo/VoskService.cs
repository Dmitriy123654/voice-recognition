using System;
using Vosk;

public class VoskService
{
    private static Model _model;
    private static readonly object _lock = new object();

    public static Model GetModel(string modelPath, bool verbose = true)
    {
        if (_model == null)
        {
            lock (_lock)
            {
                if (_model == null)
                {
                    if (verbose) Console.WriteLine("Загрузка модели...");
                    _model = new Model(modelPath);
                }
            }
        }
        return _model;
    }
} 