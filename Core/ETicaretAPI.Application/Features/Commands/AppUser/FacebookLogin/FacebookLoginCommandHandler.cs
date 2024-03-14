using ETicaretAPI.Application.Abstractions.Token;
using ETicaretAPI.Application.DTOs;
using ETicaretAPI.Application.DTOs.Facebook;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ETicaretAPI.Application.Features.Commands.AppUser.FacebookLogin
{
    public class FacebookLoginCommandHandler : IRequestHandler<FacebookLoginCommandRequest, FacebookLoginCommandResponse>
    {
        readonly UserManager<Domain.Entities.Identity.AppUser> _userManager;
        readonly ITokenHandler _tokenHandler;
        readonly HttpClient _httpClient;

        public FacebookLoginCommandHandler(UserManager<Domain.Entities.Identity.AppUser> userManager, ITokenHandler tokenHandler, IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _tokenHandler = tokenHandler;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<FacebookLoginCommandResponse> Handle(FacebookLoginCommandRequest request, CancellationToken cancellationToken)
        {
            string accessTokenResponse = await _httpClient.GetStringAsync($"https://graph.facebook.com/oauth/access_token?client_id=427352943286402&client_secret=deaa33d1a90c8b08689d5762505eb4e5&grant_type=client_credentials");

            var accessTokenJsonDocument = JsonDocument.Parse(accessTokenResponse);
            var facebookAccessTokenResponse = System.Text.Json.JsonSerializer.Deserialize<FacebookAccessTokenResponse_DTO>(accessTokenJsonDocument);

            string userAccessTokenValidation = await _httpClient.GetStringAsync($"https://graph.facebook.com/debug_token?input_token={request.AuthToken}&access_token={facebookAccessTokenResponse.AccessToken}");

            var validation = System.Text.Json.JsonSerializer.Deserialize<FacebookUserAccessTokenValidation>(userAccessTokenValidation);

            FacebookUserInfoResponse userInfo = null;
            if (validation.Data.IsValid)
            {
                string userInfoResponse = await _httpClient.GetStringAsync($"https://graph.facebook.com/me?fields=email,name&access_token={request.AuthToken}");
                userInfo =  System.Text.Json.JsonSerializer.Deserialize<FacebookUserInfoResponse>(userInfoResponse);
            }

            var info = new UserLoginInfo("FACEBOOK", validation.Data.UserId, "FACEBOOK");
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            bool result = user != null;
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(userInfo?.Email);
                if (user == null)
                {
                    user = new Domain.Entities.Identity.AppUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = userInfo?.Email,
                        UserName = userInfo?.Email,
                        NameSurname = userInfo?.Name
                    };
                    var identityResult = await _userManager.CreateAsync(user);
                    result = identityResult.Succeeded;
                }
            }
            if (result)
            {
                await _userManager.AddLoginAsync(user, info); //ASpNetUserLogins Tablosuna ekleme yapar

                var token = _tokenHandler.CreateAccessToken(5);

                return new FacebookLoginCommandResponse
                {
                    Token = token
                };
            }
            throw new Exception("Invalid external authentication.");
        }
    }
}
