using Microsoft.AspNetCore.Mvc;

namespace TicTacToe.Webapi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
