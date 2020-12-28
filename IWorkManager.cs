using System.Threading.Tasks;

namespace worker.demo
{
    public interface IWorkManager { 
        
        Task AssignWork();
    }
}