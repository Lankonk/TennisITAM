using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using System.Text;
using Humanizer;
using System.ComponentModel.DataAnnotations;
using TennisITAM.Services;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace TennisITAM.Models
{
    [Index(nameof(cu), IsUnique = true)]
    public class Usuario : IdentityUser
    {
        [Required]
        public int cu { get; set; }
        
    }
}
