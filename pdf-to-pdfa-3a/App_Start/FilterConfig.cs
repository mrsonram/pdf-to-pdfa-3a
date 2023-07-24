using System.Web;
using System.Web.Mvc;

namespace pdf_to_pdfa_3a
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
