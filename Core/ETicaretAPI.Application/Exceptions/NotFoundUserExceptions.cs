using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ETicaretAPI.Application.Exceptions
{
    public class NotFoundUserExceptions : Exception
    {
        public NotFoundUserExceptions() : base("Kullanıcı Adı veya Şifre Hatalı")
        {
        }

        public NotFoundUserExceptions(string? message) : base(message)
        {
        }

        public NotFoundUserExceptions(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
