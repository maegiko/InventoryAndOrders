using FastEndpoints;
using InventoryAndOrders.DTOs;
using InventoryAndOrders.Services;
using InventoryAndOrders.Services.Exceptions;

public class AuthLoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly AuthServices _auth;

    public AuthLoginEndpoint(AuthServices auth)
    {
        _auth = auth;
    }

    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();

        Description(b => b
            .Produces<LoginResponse>(200)
            .Produces<ApiErrorResponse>(401)
            .Produces<ErrorResponse>(400)
        );

        Summary(s =>
        {
            s.Response<ApiErrorResponse>(
                401,
                """
                - Username not matching
                - Password incorrect
                - Account doesn't exist
                """
            );
            s.Response<ErrorResponse>(
                400,
                """
                If any of the following is true:
                - Username is empty
                - Password is empty
                """
            );
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        try
        {
            LoginResponse res = _auth.Login(req.Username, req.Password);
            await Send.OkAsync(res, ct);
        }
        catch (IncorrectDetailsException ex)
        {
            await Send.ResultAsync(
                TypedResults.Json(
                    new ApiErrorResponse { Message = ex.Message },
                    statusCode: StatusCodes.Status401Unauthorized));
        }
    }
}