using AstraSystemsRental.Front.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraSystemsRental.Front.Features.Auth;

public sealed record LoginForm(string Email, string Password, bool Remember);
public sealed record ForgotPasswordForm(string Email);

public sealed class AuthController(IGatewayClient gateway, ISessionService session) : Controller
{
    [HttpGet("/auth/login")]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(session.GetToken()))
            return Redirect("/");
        return View();
    }

    [HttpPost("/auth/login")]
    public async Task<IActionResult> Login([FromForm] LoginForm form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Email) || string.IsNullOrWhiteSpace(form.Password))
        {
            ViewBag.Error = "Ingresa tu correo y contraseña.";
            return PartialView("_LoginForm", form);
        }

        var result = await gateway.LoginAsync(form.Email.Trim(), form.Password, cancellationToken);
        if (!result.Success || result.AccessToken is null)
        {
            ViewBag.Error = result.Error ?? "Credenciales inválidas.";
            return PartialView("_LoginForm", form);
        }

        session.SignIn(result.AccessToken, form.Remember);
        Response.Headers["HX-Redirect"] = "/";
        return new EmptyResult();
    }

    [HttpPost("/auth/logout")]
    public IActionResult Logout()
    {
        session.SignOut();
        return Redirect("/auth/login");
    }

    [HttpGet("/auth/forgot-password")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("/auth/forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromForm] ForgotPasswordForm form, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(form.Email))
            await gateway.RequestPasswordResetAsync(form.Email.Trim(), cancellationToken);

        ViewBag.Sent = true;
        return PartialView("_ForgotPasswordForm", form);
    }
}
