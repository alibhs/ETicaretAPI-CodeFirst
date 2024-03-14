using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ETicaretAPI.Application.DTOs.Facebook
{
    public class FacebookUserAccessTokenValidation
    {
        [JsonPropertyName("data")]
        public FacebookUserAccessTokenValidationData Data { get; set; }
    }
    public class FacebookUserAccessTokenValidationData
    {
        [JsonPropertyName("data.user_id")]
        public string UserId { get; set; }
        [JsonPropertyName("data.is_valid")]
        public bool IsValid { get; set; }
    }
}
