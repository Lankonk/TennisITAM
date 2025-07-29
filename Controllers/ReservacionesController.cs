using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TennisITAM.Models;
using TennisITAM.Services;

namespace TennisITAM.Controllers
{
    [Authorize]
    public class ReservacionesController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        
        public ReservacionesController(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> reservaTennis()
        {
            ViewData["hoy"] = SQLCode.obtenerPresente();
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return NotFound($"No se pudo cargar la cu del usuario '{_userManager.GetUserName(User)}'.");
            }
            reservacionTennis r = new reservacionTennis();
            r.idU1 = usuario.cu;
            r.hReserva = DateTime.Today;
            return View(r);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>agendarCancha(reservacionTennis reservacion) 
        {
            var usuarioIngresado = await _userManager.GetUserAsync(User);

            try
            {
                if (reservacion.idU1 != usuarioIngresado.cu)
                {
                    ModelState.AddModelError(string.Empty, "Error: Claves Unicas distintas");
                    return View("ReservaTennis");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Algun error ocurrio a la hora de verificar la clave");
                return View("ReservaTennis");
            }

            if (ModelState.IsValid)
            {
                var res = await SQLCode.validacionInfo(reservacion);
                if(res.exito)
                    return RedirectToAction("StatusReserva");
                else
                {
                    ModelState.AddModelError(string.Empty, res.errMsg);
                    return View("ReservaTennis");
                }
            }
            else
            {
                return View("ReservaTennis",reservacion);
            }
        }

        public IActionResult StatusReserva()
        {
            return View();
        }
    }
}
