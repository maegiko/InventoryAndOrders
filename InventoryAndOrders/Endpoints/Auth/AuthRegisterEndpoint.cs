using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;

public class AuthRegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
    private readonly AuthServices _auth;

    public AuthRegisterEndpoint(AuthServices auth)
    {
        _auth = auth;
    }

    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();

        Description(b => b
            .Produces<RegisterResponse>(200)
            .Produces<ApiErrorResponse>(409)
            .Produces<ErrorResponse>(400)
        );

        Summary(s =>
        {
            s.Response<ApiErrorResponse>(
                409,
                """
                Username/Email already in use.
                """
            );
            s.Response<ErrorResponse>(
                400,
                """
                If any of the following is true:
                - Username is empty
                - Email is empty
                - Password is empty
                - Email is not a valid email
                - Password is too weak
                """
            );
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        try
        {
            RegisterResponse res = _auth.Register(req.Username, req.Email, req.Password);
            await Send.OkAsync(res, ct);
        }
        catch (PasswordWeakException ex)
        {
            AddError(r => r.Password, ex.Message);
            ThrowIfAnyErrors();
        }
        catch (AccountExistsException ex)
        {
            await Send.ResultAsync(
                TypedResults.Conflict(new ApiErrorResponse { Message = ex.Message }));
        }
    }
}