using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NLog;

namespace Guohui_Wcs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PDAController : Controller
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // GET: PDAController
        public ActionResult Index()
        {
            return View();
        }

        // GET: PDAController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PDAController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PDAController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PDAController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PDAController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PDAController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PDAController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
