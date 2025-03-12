using System.Threading.Tasks;
using Vosk;

namespace VoskDemo.Core.Interfaces
{
    public interface IVoskService
    {
        Task<Model> GetModelAsync(string modelPath, bool verbose = true);
        void Cleanup();
    }
} 