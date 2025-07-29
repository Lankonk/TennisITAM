using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using System.Diagnostics.Eventing.Reader;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TennisITAM.Models;

namespace TennisITAM.Services
{
    public class SQLCode
    {
        //metodos generales que se usan en ambas reservaciones
        public static async Task<SqlConnection> agregarConexion()
        {
            SqlConnection cnn;
            try
            {
                cnn = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ReservacionesTennis;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False");
                await cnn.OpenAsync();
            }
            catch (Exception ex)
            {
                cnn = null;
            }
            return cnn;
        }
        public static async Task<(bool exito, string errMsg)> validacionInfo(reservacionTennis r)
        {
            (bool exito ,string fallaReserva) res, rUsuarios;
            try
            {
                rUsuarios = await revisarusuariosRegistrados(r);

                if (!rUsuarios.exito)
                {
                    return (false, rUsuarios.fallaReserva);
                }

                if (r.idU3 == null && r.idU4 == null)
                {
                    res = await agendarCanchaSingles(r);
                }
                else
                {
                    res = await agendarCanchaDoubles(r);
                }
                if (res.exito)
                {
                    return (res.exito, "Exito");
                }
                else
                    return (res.exito, res.fallaReserva);
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
        }

        private static async Task<(bool existe, string msg)> revisarusuariosRegistrados(reservacionTennis r)
        {
            SqlConnection c = await agregarConexion();
            SqlCommand cmd;
            object? res;
            bool registrado = false;

            if(r.idU3 == null && r.idU4 == null)
            {
                cmd = new SqlCommand("SELECT COUNT(*) FROM AspNetUsers WHERE cu IN (@cu1,@cu2);",c);
                cmd.Parameters.AddWithValue("@cu1", r.idU1);
                cmd.Parameters.AddWithValue("@cu2", r.idU2);
                res = await cmd.ExecuteScalarAsync();

                if(res != null && res != DBNull.Value)
                {
                    registrado = Convert.ToInt64(res) == 2;
                }
                await cmd.DisposeAsync();
                await c.CloseAsync();
                if (registrado)
                    return (true, string.Empty);
                else
                    return (false, "Alguna de las claves no esta registrada en el sistema. Favor de registrarse");
            }
            else
            {
                cmd = new SqlCommand("SELECT COUNT(*) FROM AspNetUsers WHERE cu IN (@cu1,@cu2,@cu3,@cu4);",c);
                cmd.Parameters.AddWithValue("@cu1", r.idU1);
                cmd.Parameters.AddWithValue("@cu2", r.idU2);
                cmd.Parameters.AddWithValue("@cu3", r.idU3);
                cmd.Parameters.AddWithValue("@cu4", r.idU4);
                res = await cmd.ExecuteScalarAsync();

                if (res != null && res != DBNull.Value)
                {
                    registrado = Convert.ToInt64(res) == 4;
                }
                await cmd.DisposeAsync();
                await c.CloseAsync();
                if (registrado)
                    return (true, string.Empty);
                else
                    return (false, "Alguna de las claves no esta registrada en el sistema. Favor de registrarse");
            }
        }

        private static async Task<(bool exito, string msg)> correosReserva(reservacionTennis r)
        {
            /*implementar los correos de evidencia de reserva
             * Tambien implementar los correos de que enciendan las luces si es muy tarde
             */
            throw new NotImplementedException();
        }
        
        private static async Task<(bool disp, string msg)> canchaLibres(SqlConnection c, string cancha, DateTime dtOriginal)//solo determina que esa cancha en especifico no este reservada
        {
            bool disponible = true;
            DateTime hQuerida = dtOriginal.AddHours(-1.0).AddSeconds(1.0);
            DateTime termina = hQuerida.AddHours(2.0).AddSeconds(-2.0);
            int idRec = await getidRecReservado(cancha);
            SqlCommand cmd = new SqlCommand(String.Format("SELECT id_reservacion FROM reservacion_cancha WHERE (hora_reservada BETWEEN @start AND @end) AND cancha_reservada = @cancha;"), c);
            cmd.Parameters.AddWithValue("@start", hQuerida);
            cmd.Parameters.AddWithValue("@end", termina);
            cmd.Parameters.AddWithValue("@cancha",idRec);
            SqlDataReader dr = await cmd.ExecuteReaderAsync();

            if (await dr.ReadAsync())
                disponible = false;

            if (disponible)
            {
                await dr.CloseAsync();
                cmd = new SqlCommand(String.Format("SELECT id_reservacion FROM reservacion_cancha_dobles WHERE (hora_reservada BETWEEN @start AND @end) AND cancha_reservada = @cancha;"), c);
                cmd.Parameters.AddWithValue("@start", hQuerida);
                cmd.Parameters.AddWithValue("@end", termina);
                cmd.Parameters.AddWithValue("@cancha", idRec);
                dr = await cmd.ExecuteReaderAsync();
                if (await dr.ReadAsync())
                    disponible = false;
            }

            await dr.CloseAsync();
            if (disponible)
                return (true, string.Empty);
            else
                return (false, "No hay canchas libres en el horario seleccionado");
        }
        private static async Task<int> getidRecReservado(string s)
        {
            int res = -1;
            SqlConnection c = await agregarConexion();
            SqlCommand cmd = new SqlCommand(String.Format("SELECT id FROM recurso WHERE nom_recurso = @rec;"), c);
            cmd.Parameters.AddWithValue("@rec", s);
            SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (await dr.ReadAsync())
            {
                res = dr.GetInt32(0);
            }
            dr.Close();
            c.Close();
            return res;
        }
        private static async Task<(bool disp, string msg)> problemasHorarios(DateTime t)//false si no hay problemas
        {
            bool existenProblemas = false;
            bool bloqueoTot = horarioBloqueoTotal(t);
            var bloqueoParcial = await horarioBloqueoParcial(t);
            if (bloqueoTot || bloqueoParcial)
                existenProblemas = true;

            if (existenProblemas)
                return (existenProblemas, "El horario que seleccionaste no esta disponible");
            else
                return (existenProblemas, string.Empty);
        }
        private static bool horarioBloqueoTotal(DateTime t)
        {
            TimeSpan temp = t.TimeOfDay;
            bool bloqueado = false;

            if (t < DateTime.Now || temp >= new TimeSpan(22, 0, 0) || temp <= new TimeSpan(7, 0, 0))
            {
                bloqueado = true;
            }
            if (t.DayOfWeek == DayOfWeek.Thursday && (temp >= new TimeSpan(13, 0, 0) && temp < new TimeSpan(15, 0, 0)))
            {
                bloqueado = true;
            }

            if (t.DayOfWeek == DayOfWeek.Friday && (temp >= new TimeSpan(13, 0, 0) && temp < new TimeSpan(15, 0, 0)))
            {
                bloqueado = true;
            }

            if (t.DayOfWeek == DayOfWeek.Friday && (temp >= new TimeSpan(7, 0, 0) && temp < new TimeSpan(9, 0, 0)))
            {
                bloqueado = true;
            }

            return bloqueado;
        }       
        private static async Task<bool> horarioBloqueoParcial(DateTime dtOriginal)
        {
            bool existenProblemas = false;
            DateTime principio = dtOriginal.AddHours(-1.0).AddSeconds(1.0);
            DateTime fin = principio.AddHours(2).AddSeconds(-2.0);
            TimeSpan t = dtOriginal.TimeOfDay;
            SqlConnection c = await agregarConexion();
            SqlCommand cmd;
            SqlDataReader? dr = null;
            
            cmd = new SqlCommand(String.Format("SELECT hora_reservada FROM reservacion_cancha WHERE hora_reservada BETWEEN @start AND @end UNION ALL SELECT hora_reservada FROM reservacion_cancha_dobles WHERE hora_reservada BETWEEN @start AND @end;"), c);
            cmd.Parameters.AddWithValue("@start", principio);
            cmd.Parameters.AddWithValue("@end", fin);
            
            if (principio.DayOfWeek == DayOfWeek.Monday || principio.DayOfWeek == DayOfWeek.Tuesday || principio.DayOfWeek == DayOfWeek.Wednesday)
            {
                    if (t >= new TimeSpan(14, 0, 0) && t < new TimeSpan(16, 0, 0))
                    {
                    dr = await cmd.ExecuteReaderAsync();
                    if (await dr.ReadAsync())
                            existenProblemas = true;
                    }
                    else
                    {
                        if (principio.DayOfWeek == DayOfWeek.Tuesday && t >= new TimeSpan(10, 0, 0) && t < new TimeSpan(12, 0, 0))
                        {
                        dr = await cmd.ExecuteReaderAsync();
                        if (await dr.ReadAsync())
                                existenProblemas = true;
                        }
                    }
            }

            if (!existenProblemas && (principio.DayOfWeek == DayOfWeek.Wednesday || principio.DayOfWeek == DayOfWeek.Thursday))
            {
                    if (t >= new TimeSpan(7, 0, 0) && t < new TimeSpan(11, 0, 0))
                    {
                    dr = await cmd.ExecuteReaderAsync();
                    if (await dr.ReadAsync())
                            existenProblemas = true;
                    }

                    if (principio.DayOfWeek == DayOfWeek.Thursday && t >= new TimeSpan(11, 0, 0) && t < new TimeSpan(13, 0, 0))
                    {
                    dr = await cmd.ExecuteReaderAsync();
                    if (await dr.ReadAsync())
                            existenProblemas = true;
                    }

            }

            if (!existenProblemas && principio.DayOfWeek == DayOfWeek.Friday)
            {
                    if (t >= new TimeSpan(10, 0, 0) && t < new TimeSpan(13, 0, 0))
                    {
                    dr = await cmd.ExecuteReaderAsync();
                    if (await dr.ReadAsync())
                            existenProblemas = true;
                    }
            }
            if (!existenProblemas && principio.DayOfWeek == DayOfWeek.Saturday)
            {
                    if (t >= new TimeSpan(8, 0, 0) && t < new TimeSpan(9, 30, 0))
                    {
                    dr = await cmd.ExecuteReaderAsync();
                    if (await dr.ReadAsync())
                            existenProblemas = true;
                    }
            }
            if(dr != null)
                await dr.CloseAsync();

            await cmd.DisposeAsync();
            await c.CloseAsync();

            return existenProblemas;
        }
        private static async Task<(bool valido, string msg)> checarClaves(SqlConnection c, reservacionTennis r)//checar que solo tengan una reservacion valida
        {
            bool puedenReservar = true;
            DateTime temp;
            SqlCommand cmd;
            string querySingles = "SELECT hora_reservada FROM reservacion_cancha WHERE ((id_usuario1 = @cu1 AND id_usuario2 = @cu2) OR(id_usuario1 = @cu2 AND id_usuario2 = @cu1)) AND hora_reservada >= @dia UNION ALL SELECT hora_reservada FROM reservacion_cancha_dobles WHERE (((id_usuario1 = @cu1 AND id_usuario2 = @cu2) OR (id_usuario1 = @cu2 AND id_usuario2 = @cu1)) OR ((id_usuario1 = @cu1 AND id_usuario3 = @cu2) OR (id_usuario1 = @cu2 AND id_usuario3 = @cu1)) OR ((id_usuario1 = @cu1 AND id_usuario4 = @cu2) OR (id_usuario1 = @cu2 AND id_usuario4 = @cu1)) OR ((id_usuario2 = @cu1 AND id_usuario3 = @cu2) OR (id_usuario2 = @cu2 AND id_usuario3 = @cu1)) OR ((id_usuario2 = @cu1 AND id_usuario4 = @cu2) OR (id_usuario2 = @cu2 AND id_usuario4 = @cu1)) OR ((id_usuario3 = @cu1 AND id_usuario4 = @cu2) OR (id_usuario3 = @cu2 AND id_usuario4 = @cu1))) AND hora_reservada >= @dia;";

            //querydoubles falta checarlo de manera mas exhaustiva por el momento parece funcionar
            string queryDoubles = "SELECT hora_reservada FROM reservacion_cancha WHERE (((id_usuario1 = @cu1 AND id_usuario2 = @cu2) OR (id_usuario1 = @cu2 AND id_usuario2 = @cu1)) OR((id_usuario1 = @cu1 AND id_usuario2 = @cu3) OR (id_usuario1 = @cu3 AND id_usuario2 = @cu1)) OR((id_usuario1 = @cu1 AND id_usuario2 = @cu4) OR (id_usuario1 = @cu4 AND id_usuario2 = @cu1)) OR((id_usuario1 = @cu2 AND id_usuario2 = @cu3) OR (id_usuario1 = @cu3 AND id_usuario2 = @cu2)) OR((id_usuario1 = @cu2 AND id_usuario2 = @cu4) OR (id_usuario1 = @cu4 AND id_usuario2 = @cu2)) OR((id_usuario1 = @cu3 AND id_usuario2 = @cu4) OR (id_usuario1 = @cu4 AND id_usuario2 = @cu3))) AND hora_reservada >= @dia UNION ALL SELECT hora_reservada FROM reservacion_cancha_dobles WHERE (id_usuario1 IN(@cu1, @cu2, @cu3, @cu4) AND id_usuario2 IN(@cu1, @cu2, @cu3, @cu4) AND id_usuario3 IN(@cu1, @cu2, @cu3, @cu4) AND id_usuario4 IN(@cu1, @cu2, @cu3, @cu4)) AND (@cu1 IN(id_usuario1, id_usuario2, id_usuario3, id_usuario4) AND @cu2 IN(id_usuario1, id_usuario2, id_usuario3, id_usuario4) AND @cu3 IN(id_usuario1, id_usuario2, id_usuario3, id_usuario4) AND @cu4 IN(id_usuario1, id_usuario2, id_usuario3, id_usuario4))AND hora_reservada >= @dia;";

            if (r.idU3 == null || r.idU4 == null)
            {
                cmd = new SqlCommand(querySingles, c);
                cmd.Parameters.AddWithValue("@cu1", r.idU1);
                cmd.Parameters.AddWithValue("@cu2", r.idU2);
                cmd.Parameters.AddWithValue("@dia", r.hReserva.Date);
            }
            else
            {
                cmd = new SqlCommand(queryDoubles, c);
                cmd.Parameters.AddWithValue("@cu1", r.idU1);
                cmd.Parameters.AddWithValue("@cu2", r.idU2);
                cmd.Parameters.AddWithValue("@cu3", r.idU3);
                cmd.Parameters.AddWithValue("@cu4", r.idU4);
                cmd.Parameters.AddWithValue("@dia", r.hReserva.Date);
            }

            SqlDataReader dr = await cmd.ExecuteReaderAsync();
            while (await dr.ReadAsync() && puedenReservar)
            {
                temp = dr.GetDateTime(0);

                if (temp.Date == r.hReserva.Date && (temp.Hour + 1 == r.hReserva.Hour || temp.Hour == r.hReserva.Hour))
                {
                    puedenReservar = false;
                }
            }
            dr.Close();

            if (puedenReservar)
                return (puedenReservar, string.Empty);
            else
                return (puedenReservar, "Las claves proporcionadas ya tienen reservas o no estan registradas");
        }
        public static string obtenerPresente()
        {
            return DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "T" + DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString();
        }

        //Reservas Singles
        private static async Task<(bool exito, string errMsg)> agendarCanchaSingles(reservacionTennis r)
        {
            string errorMsg = string.Empty;
            //if (Int32.TryParse(cu1, out int id1) && Int32.TryParse(cu2, out int id2) && DateTime.TryParse(date, out DateTime temp))
            //{
            if(r.idU1 != r.idU2)
            {
                
                SqlConnection con = await SQLCode.agregarConexion();
                var canchaLibre = await canchaLibres(con, r.recReservado, r.hReserva);
                var probHorarios = await problemasHorarios(r.hReserva);
                var claves = await checarClaves(con, r);
                if (canchaLibre.disp && !probHorarios.disp  && claves.valido )
                {
                    r.Id = await crearIdReservaSingles(con) + 1;
                    return await reservarCancha(con, r);
                }
                else
                {
                    errorMsg = canchaLibre.msg +" "+ probHorarios.msg +" "+ claves.msg;
                }
                    con.Close();
            }
            else
            {
                return (false, "Se necesitan dos claves distintas");
            }
                //}
                return (false, errorMsg);

        }
        private static async Task<int> crearIdReservaSingles(SqlConnection c)
        {
            int res = -1;

            SqlCommand cmd = new SqlCommand(String.Format("SELECT TOP 1 id_reservacion FROM reservacion_cancha ORDER BY id_reservacion DESC"), c);
            SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (await dr.ReadAsync())
            {
                res =(int) dr.GetInt64(0);
            }
            dr.Close();
            return res;
        }
        private static async Task<(bool exito, string msg)> reservarCancha(SqlConnection c,reservacionTennis r)
        {
            bool exito = false;
            int idRecReservado = await getidRecReservado(r.recReservado), res;
            SqlCommand cmd = new SqlCommand(String.Format("INSERT INTO reservacion_cancha (id_reservacion, id_usuario1, id_usuario2, hora_reservada, cancha_reservada) VALUES(@id,@cu1,@cu2,@hora,@idRec);"), c);
            cmd.Parameters.AddWithValue("@id",r.Id);
            cmd.Parameters.AddWithValue("@cu1", r.idU1);
            cmd.Parameters.AddWithValue("@cu2", r.idU2);
            cmd.Parameters.AddWithValue("@hora", r.hReserva);
            cmd.Parameters.AddWithValue("@idRec", idRecReservado);
            try
            {
                res = await cmd.ExecuteNonQueryAsync();
                exito = true;
            }
            catch (Exception ex)
            {
                exito = false;
                return(exito, ex.Message);
            }
            return (exito, string.Empty);
        }
        //Reservas Doubles
        private static async Task<(bool exito, string errMsg)> agendarCanchaDoubles(reservacionTennis r) 
        {
            HashSet<int> temp = new HashSet<int>();
            string errorMsg = string.Empty;
            try
            {
                temp.Add(r.idU1);
                temp.Add(r.idU2);
                try
                {
                    temp.Add((int)r.idU3);
                    temp.Add((int)r.idU4);
                }
                catch(Exception e)
                {
                    return (false, "Faltaron claves unicas por llenar");
                }

                if (temp.Count == 4)
                {
                    SqlConnection con = await SQLCode.agregarConexion();
                    var canchaLibre = await canchaLibres(con, r.recReservado, r.hReserva);
                    var probHorarios = await problemasHorarios(r.hReserva);
                    var claves = await checarClaves(con, r);
                    if (canchaLibre.disp && !probHorarios.disp && claves.valido)
                    {
                        r.Id = await crearIdReservaDobles(con) + 1;
                        return await reservarCanchaDobles(con, r);
                    }
                    else
                    {
                        errorMsg = canchaLibre.msg +". "+ probHorarios.msg + ". "+ claves.msg;
                    }
                    con.Close();
                }
                else
                {
                    return (false, "Para reservar para dobles se necesitan 4 claves distinas");
                }
            }
            catch (Exception e)
            {
                return (false,e.Message);
            }
            return (false,errorMsg);
        }
        private static async Task<int> crearIdReservaDobles(SqlConnection c)
        {
            int res = -1;

            SqlCommand cmd = new SqlCommand(String.Format("SELECT TOP 1 id_reservacion FROM reservacion_cancha_dobles ORDER BY id_reservacion DESC"), c);
            SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (await dr.ReadAsync())
            {
                res = (int)dr.GetInt64(0);
            }
            dr.Close();
            return res;
        }

        private static async Task<(bool exito, string errMsg)> reservarCanchaDobles(SqlConnection c, reservacionTennis r)
        {
            bool exito = false;
            int idRecReservado = await getidRecReservado(r.recReservado), res;
            SqlCommand cmd = new SqlCommand(String.Format("INSERT INTO reservacion_cancha_dobles (id_reservacion, id_usuario1, id_usuario2,id_usuario3, id_usuario4, hora_reservada, cancha_reservada) VALUES(@idRes, @cu1, @cu2, @cu3, @cu4, @hora, @cancha);"), c);
            cmd.Parameters.AddWithValue("@idRes", r.Id);
            cmd.Parameters.AddWithValue("@cu1", r.idU1);
            cmd.Parameters.AddWithValue("@cu2", r.idU2);
            cmd.Parameters.AddWithValue("@cu3", r.idU3);
            cmd.Parameters.AddWithValue("@cu4", r.idU4);
            cmd.Parameters.AddWithValue("@hora", r.hReserva);
            cmd.Parameters.AddWithValue("@cancha", idRecReservado);
            try
            {
                res = await cmd.ExecuteNonQueryAsync();
                exito = true;
            }
            catch (Exception ex)
            {
                exito = false;
                return(exito, ex.Message);
            }
            return (exito, string.Empty);
        }

        //Usuarios

        /*public static bool verificarContra(string passDado, string passVerdadero)
        {
            byte[] dbPass = Convert.FromBase64String(passVerdadero);
            byte[] salt = new byte[16];

            Buffer.BlockCopy(dbPass, 0, salt, 0, 16);
            var pbkdf2 = new Rfc2898DeriveBytes(passDado, salt, Usuario.vecesHash, HashAlgorithmName.SHA512);
            byte[] hash = pbkdf2.GetBytes(64);

            for (int i = 0; i < 64; i++)
            {
                if (dbPass[i + 16] != hash[i])
                    return false;
            }
            return true;
        }*/

        /*public static int altaUsuario(string id, string correo, string passNoHash)
        {
            int res = -1;

            if (Int32.TryParse(id, out int cu) && revisionParametros(cu, correo, passNoHash))
            {
                Usuario u = new Usuario(cu, correo, creacionContra(passNoHash), false);
                SqlConnection conn = SQLCode.agregarConexion();
                SqlCommand cmd = new SqlCommand(String.Format("INSERT INTO Usuario (Id, correo, contra, admin) VALUES({0},'{1}','{2}',FALSE);", u.Id, u.correo, u.contra), conn);
                res = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return res;
        }*/
        /*private static bool revisionParametros(int cu, string correo, string passNoHash)
        {
            bool bandera = true;//Es true que los parametros son validos
            char[] email = correo.ToCharArray();
            char[] terminacion = "@itam.mx".ToCharArray();
            int indEmail = -1, i = 0;
            SqlConnection c = SQLCode.agregarConexion();
            SqlCommand cmd;
            SqlDataReader dr;

            if (busquedaUsuario(cu, c))
                bandera = false;//ya no son validos

            if (bandera)
            {
                while (i < email.Length && indEmail == -1)
                {
                    if (email[i] == '@' && i > 0)
                    {
                        indEmail = i;
                    }   
                    else
                    {
                        if ((email[i] == '-' && email[i + 1] == '-') || (email[i] == '/' && email[i + 1] == '*'))
                        {
                            indEmail = -2;
                            bandera = false;
                        }
                        i++;
                    }
                }
                if (indEmail > 0 && indEmail + 7 == email.Length - 1)
                {
                    for (int j = 0; j < terminacion.Length; j++)
                    {
                        if (email[j + indEmail] != terminacion[j])
                        {
                            bandera = false;
                        }
                    }
                }
            }
            if (bandera)
            {
                cmd = new SqlCommand(String.Format("SELECT Id FROM Usuario WHERE CORREO = '{0}'", correo), c);
                dr = cmd.ExecuteReader();

                if (dr.HasRows)
                {
                    bandera = false;
                }
            }

            return bandera;//bandera final
        }
        private static String creacionContra(string pass)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            var pbkdf2 = new Rfc2898DeriveBytes(pass, salt, Usuario.vecesHash, HashAlgorithmName.SHA512);
            byte[] hash = pbkdf2.GetBytes(64);
            byte[] final = new byte[80];

            Buffer.BlockCopy(salt, 0, final, 0, 16);
            Buffer.BlockCopy(hash, 0, final, 16, 64);

            return Convert.ToBase64String(final);
        }
        */
        public static async Task<bool> yaExisteUsuario(int cu, string correo)// True si ya esta la cu o el correo
        {
            SqlConnection c = await agregarConexion();
            
            bool busqueda = await busquedaCu(cu,c),
                correoYa = await correoYaRegistrado(correo,c);

            c.Close();
            return busqueda || correoYa;
        }

        private static async Task<bool> busquedaCu(int cu, SqlConnection c)
        {
            int res = -1;
            SqlCommand cmd = new SqlCommand(String.Format("SELECT cu FROM AspNetUsers WHERE cu = @cu;"), c);
            cmd.Parameters.AddWithValue("@cu",cu);
            SqlDataReader dr = await cmd.ExecuteReaderAsync();

            if(await dr.ReadAsync())
            {
                res = dr.GetInt32(0);
            }
            dr.Close();
            return res != -1;
        }

        private static async Task<bool> correoYaRegistrado(string correo, SqlConnection c)
        {
            bool yaReg = false;
            string correoNorm = correo.ToUpper().Trim();
            SqlCommand cmd = new SqlCommand(String.Format("SELECT NORMALIZEDEMAIL FROM AspNetUsers WHERE NORMALIZEDEMAIL = '@correo';", correoNorm),c);
            cmd.Parameters.AddWithValue("@correo", correo);
            SqlDataReader dr = await cmd.ExecuteReaderAsync();
            if (await dr.ReadAsync())
            {
                yaReg = true;
            }
            dr.Close ();
            return yaReg;
        }
    }
}
