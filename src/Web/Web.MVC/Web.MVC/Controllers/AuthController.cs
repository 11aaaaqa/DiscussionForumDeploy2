﻿using System.Net;
using System.Text;
using System.Text.Json;
using DNTCaptcha.Core;
using GeneralClassesLib.ApiResponses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Web.MVC.DTOs.Auth;
using Web.MVC.DTOs.ResetPassword;
using Web.MVC.Models.ApiResponses;

namespace Web.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<AuthController> logger;
        private readonly IConfiguration configuration;
        private readonly string url;
        private static readonly Counter UsersConfirmedEmailCounter = Metrics.CreateCounter
            ("users_confirmed_email", "Number of users confirmed their email.");

        public AuthController(IHttpClientFactory httpClientFactory, ILogger<AuthController> logger, IConfiguration configuration)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.configuration = configuration;
            url = (string.IsNullOrEmpty(configuration["Url:Port"]))
                ? $"{configuration["Url:Protocol"]}://{configuration["Url:HostName"]}"
                : $"{configuration["Url:Protocol"]}://{configuration["Url:HostName"]}:{configuration["Url:Port"]}";
        }

        [Route("auth/register")]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [ValidateDNTCaptcha(ErrorMessage = "Ошибка в капче")]
        [Route("auth/register")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            logger.LogInformation("Register method start working");

            if (ModelState.IsValid)
            {
                using HttpClient client = httpClientFactory.CreateClient();

                model.Uri = new ConfirmEmailMethodUri
                {
                    Protocol = configuration["Uri:Protocol"],
                    DomainName = configuration["Uri:DomainName"],
                    Controller = "Auth",
                    Action = "ConfirmEmail"
                };

                using StringContent jsonContent = new(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

                logger.LogInformation("Http client was created and model was serialized");
                
                var response = await client.PostAsync(
                    $"{url}/api/Auth/register",
                    jsonContent);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    logger.LogInformation("Response has success status code");
                    return View("Confirmation");
                }
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    logger.LogError("User already exists, end method");
                    ModelState.AddModelError(string.Empty, "Такой пользователь уже существует");
                }
                else
                {
                    logger.LogError("Response hasn't success status code, end method");
                    ModelState.AddModelError(string.Empty,"Что-то пошло не так, попробуйте ещё раз");
                }
            }
            else
            {
                logger.LogError("Model state isn't valid, end method");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            logger.LogInformation("ConfirmEmail method start working");
            using HttpClient client = httpClientFactory.CreateClient();

            logger.LogInformation("Http client was created");

            var response = await client.GetAsync($"{url}/api/Auth/confirmEmail?token={token}&userId={userId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                UsersConfirmedEmailCounter.Inc();

                var userResponse = await client.GetAsync($"{url}/api/User/GetById?uid={userId}");
                if (userResponse.StatusCode != HttpStatusCode.OK)
                {
                    return View("ActionError");
                }

                var user = await userResponse.Content.ReadFromJsonAsync<UserResponse>();
                
                var authResponse = await client.GetAsync(
                    $"{url}/api/User/AuthUserByUserName?userName={user.UserName}");
                if (authResponse.StatusCode != HttpStatusCode.OK)
                {
                    return View("ActionError");
                }

                var authenticated = await authResponse.Content.ReadFromJsonAsync<AuthenticatedResponse>();
                AuthenticateUser(authenticated!.Token);

                logger.LogInformation("Response has success status code, end method");
                return View();
            }
            else
            {
                logger.LogError("Response hasn't success status code, end method");
                return View("ActionError");
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();

                var userResponse = await httpClient.GetAsync(
                    $"{url}/api/User/GetByUserNameOrEmail?userNameOrEmail={model.UserNameOrEmail}");
                if (userResponse.StatusCode == HttpStatusCode.OK)
                {
                    var user = await userResponse.Content.ReadFromJsonAsync<GetUserResponse>();
                    if (user.EmailConfirmed == false)
                        return RedirectToAction("ConfirmEmailPrepare", routeValues: new { UserId = user.Id });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Такого пользователя не существует");
                    return View(model);
                }
                
                using StringContent jsonContent = new(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{url}/api/Auth/login", jsonContent);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadFromJsonAsync<AuthenticatedResponse>();

                    AuthenticateUser(content!.Token);

                    if (!string.IsNullOrEmpty(returnUrl) && !returnUrl.Contains("auth/register") && !returnUrl.ToLower().Contains("auth/login"))
                    {
                        return LocalRedirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
                if(response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    logger.LogError("User with current password doesn't exist, end method");
                    ModelState.AddModelError(string.Empty, "Пользователя с таким паролем не существует");
                }
                else
                {
                    logger.LogCritical("Something went wrong, user wasn't authenticated, end method");
                    ModelState.AddModelError(string.Empty, "Что-то пошло не так, попробуйте ещё раз");
                }
            }
            else
            {
                logger.LogError("Model wasn't valid, end method");
            }
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout(string returnUrl)
        {
            logger.LogInformation("Logout method starts working");
            using HttpClient httpClient = httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync(
                $"{url}/api/Token/revoke?userName={HttpContext.User.Identity.Name}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Response.Cookies.Delete("accessToken");

                logger.LogInformation("User was successfully logged out");
            }
            else
            {
                logger.LogCritical("User with current username doesn't exist, end method");
            }

            if (!string.IsNullOrEmpty(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [Route("auth/forgot-password")]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [Route("auth/forgot-password")]
        [ValidateDNTCaptcha(ErrorMessage = "Ошибка в капче")]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (ModelState.IsValid)
            {
                using var httpClient = httpClientFactory.CreateClient();

                model.Uri = new ForgotPasswordUri()
                {
                    Protocol = configuration["Uri:Protocol"],
                    DomainName = configuration["Uri:DomainName"],
                    Controller = "Auth",
                    Action = "ResetPassword"
                };

                using StringContent jsonContent = new(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"{url}/api/Auth/ForgotPassword",
                    jsonContent);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return View("ForgotPasswordConfirmation");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty,error);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string userId)
        {
            var model = new ResetPasswordDto { Token = token, UserId = userId };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (ModelState.IsValid)
            {
                using var httpClient = httpClientFactory.CreateClient();
                using StringContent jsonContent = new(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"{url}/api/Auth/ResetPassword",
                    jsonContent);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return View("Success");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty,error);
                }
            }
            return View(model);
        }

        [Route("auth/email-is-not-confirmed")]
        [HttpGet]
        public IActionResult ConfirmEmailPrepare(ConfirmEmailPrepareDto model)
        {
            return View(model);
        }

        [Route("auth/email-is-not-confirmed")]
        [ValidateDNTCaptcha(ErrorMessage = "Ошибка в капче")]
        [HttpPost]
        public async Task<IActionResult> ConfirmEmailPrepare(string userId)
        {
            if (ModelState.IsValid)
            {
                using var httpClient = httpClientFactory.CreateClient();
                var uri = new ConfirmEmailMethodUri
                {
                    Protocol = configuration["Uri:Protocol"],
                    DomainName = configuration["Uri:DomainName"],
                    Controller = "Auth",
                    Action = "ConfirmEmail"
                };

                using StringContent jsonContent = new(JsonSerializer.Serialize(uri), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"{url}/api/User/SendEmailConfirmationLink?userId={userId}",
                    jsonContent);
                if (response.IsSuccessStatusCode)
                    return View("Confirmation");

                return View("ActionError");
            }
            return View(new ConfirmEmailPrepareDto{UserId = userId});
        }

        private void AuthenticateUser(string jwtToken)
        {
            Response.Cookies.Append("accessToken", jwtToken, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMonths(2),
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = true
            });
        }
    }
}
