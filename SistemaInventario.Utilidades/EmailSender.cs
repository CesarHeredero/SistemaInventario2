using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SistemaInventario.Utilidades
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Execute(subject, htmlMessage, email);
        }

        public Task Execute(string subject, string mensaje, string email)
        {
            MailMessage em = new MailMessage();
            em.To.Add(email);
            em.Subject = subject;
            em.Body = mensaje;
            em.From = new MailAddress("cesarheredero@outlook.com");
            em.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient("smtp.sendgrid.net");
            smtp.Port = 587;
            smtp.UseDefaultCredentials = true;
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential("apikey", "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");

            return smtp.SendMailAsync(em);
        }
    }


}
