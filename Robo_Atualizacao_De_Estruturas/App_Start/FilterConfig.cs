using System.Web;
using System.Web.Mvc;

namespace Robo_Atualizacao_De_Estruturas
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
