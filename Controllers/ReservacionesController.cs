using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NuGet.Packaging.Rules;
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
        public async Task<IActionResult> reservaTennis()//metodo para llenar la clave unica automaticamente
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

        [HttpGet]
        public async Task<IActionResult> cancelarReserva()//metodo para llenar la clave unica automaticamente
        {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> cancelacionReserva(reservacionTennis reservacion, bool checkboxDoblesCancelar)//true si es dobles; false si no
        {
            var usuarioIngresado = await _userManager.GetUserAsync(User);
            try
            {
                if (reservacion.idU1 != usuarioIngresado.cu)
                {
                    ModelState.AddModelError(string.Empty, "Error: Claves Unicas distintas");
                    return View("cancelarReserva");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Algun error ocurrio a la hora de verificar la clave");
                return View("cancelarReserva");
            }

            
            if (ModelState.IsValid)
            {
                var res = await SQLCode.cancelarReserva(reservacion, checkboxDoblesCancelar);
                if (res.exito)
                    return RedirectToAction("StatusReserva");
                else
                {
                    ModelState.AddModelError(string.Empty, res.errMsg);
                    return View("cancelarReserva");
                }
            }
            else
                return View("cancelarReserva");
        }
    }
}
