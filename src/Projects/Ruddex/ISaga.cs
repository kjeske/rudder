using System.Threading.Tasks;

namespace Ruddex
{
    public interface ISaga
    {
        Task OnNext(object action);
    }
}
