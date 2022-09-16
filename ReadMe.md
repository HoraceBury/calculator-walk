# Calculator Lambda with Function URL 

## Scaffold minimal Lambda

dotnet new serverless.AspNetCoreMinimalAPI -n Calculator

## Add .gitignore

Set-Content .\Calculator\.gitignore "**/bin`n**/obj`n**/publish`n*.zip" -NoNewLine

## Create initial commit

cd .\Calculator\
git init
git add .
git commit -m "initial commit"

## Restore nuget packages

cd .\src\Calculator\
dotnet restore

## Open in VSCode

code ../..

## Generate build and debug assets

Either use the VSCode alert which opens in the bottom right or hit F1 and select ".NET: Generate Assets for Build and Debug"

* https://github.com/dotnet/docs/issues/6408#issuecomment-404929417

## Test locally in browser

Hit F5 to visit "https://localhost:5001/" and see "Welcome to running ASP.NET Core Minimal API on AWS Lambda" on the page
Visit "https://localhost:5001/calculator/add/1/2" and see "3" on the page

## Test locally in Postman

Create a GET request with URL "https://localhost:5001/" and Send
The response body should be "Welcome to running ASP.NET Core Minimal API on AWS Lambda" in the response body
Change the URL to "https://localhost:5001/calculator/add/1/2" and Send
The response body should be "3"

If the call fails disabled SSL verification

* http://1.117.35.160/docs/sending-requests/certificates/

## Create Lambda function with Function URL

Visit Lambda console
Click "Create function"
Enter a function name
Select Runtime .NET 6
Expand Advanced settings and check Enable function URL
Check Auth type NONE
Click Create function
Under Runtime settings click Edit
Change the Handler value to the name of your project (above this was "Calculator")
Click Save

## Disable ReadyToRun

Set the .csproj value "PublishReadyToRun" to false

## Set the Lambda event source

In Program.cs change "LambdaEventSource.RestApi" to "LambdaEventSource.HttpApi"

## Build the project

Remove-Item .\publish\ -Recurse -Force; dotnet publish -c Debug --output=publish; Compress-Archive .\publish\* publish.zip -Force

## Upload the build zip

In the Lambda Function console, click Upload from and choose .zip file
Click Upload and browse to the publish.zip file
Click Save

## Test in browser

In the Lambda function console, click the link under "Function URL" in the Function overview panel
A new tab should open showing "Welcome to running ASP.NET Core Minimal API on AWS Lambda"
Append "/calculator/add/1/2" to the URL and see "3" on the page

## Test in Postman

Create a GET request with the URL under "Function URL" in the Function overview panel and Send
The response body should be "Welcome to running ASP.NET Core Minimal API on AWS Lambda"
Append "/calculator/add/1/2" to the URL and Send
The response body should be "3"

## Check CloudWatch logs

Open the CloudWatch console
Under Log groups, select "/aws/lambda/CleanCalculator"
Select the most recent log stream
See "1 plus 2 is 3"

## Add middleware logging

Create class "src\mini\Middleware\LogMiddleware.cs" with content:
```
public class LogMiddleware
{
    private readonly ILogger<LogMiddleware> logger;
    private readonly RequestDelegate next;

    public LogMiddleware(RequestDelegate _next, ILogger<LogMiddleware> _logger)
    {
        next = _next;
        logger = _logger;
    }

    public async Task Invoke(HttpContext context)
    {
        logger.LogInformation($"Invoked middleware. Auth header isnull: '{string.IsNullOrEmpty(context.Request.Headers.Authorization)}'");
        await next(context);
    }
}
```

In Program.cs, after `app.UseAuthorization();` insert `app.UseMiddleware<LogMiddleware>();`:
```
app.UseAuthorization();
app.UseMiddleware<LogMiddleware>();
app.MapControllers();
```

## Rebuild and publish to console

Remove-Item .\publish\ -Recurse -Force; dotnet publish -c Debug --output=publish; Compress-Archive .\publish\* publish.zip -Force

In the Lambda Function console, click Upload from and choose .zip file
Click Upload and browse to the publish.zip file
Click Save

## Test to see logs in CloudWatch

Repeat the GET request in Postman
Refresh the log stream in CloudWatch
Select the latest stream
See "Invoked middleware. Auth header isnull: 'True'" in the log

## Add OAuth2 Introspection nuget

In the .csproj file add page `<PackageReference Include="IdentityModel.AspNetCore.OAuth2Introspection" Version="6.1.0" />`

## Restore packages

Accept the "There are unresolved dependencies. Please execute the restore command to continue." alert in the bottom right, or
Hit F1 and select ".NET: Restore All Projects"

## Implement authorization

In `CalculatorController.cs` add using statement `using Microsoft.AspNetCore.Authorization;`
Above the `Add()` method add the method attribute `[Authorize]`

## Introduce OAuth2 introspection into pipeline

In `Program.cs` add using statement `using IdentityModel.AspNetCore.OAuth2Introspection;`

After `builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);` add:
```
builder.Services.AddAuthentication(OAuth2IntrospectionDefaults.AuthenticationScheme)
    .AddOAuth2Introspection(options =>
    {
        options.Authority = "[oauth url]"; // eg: https://dev-api.airproducts.com/oauth

        options.ClientId = "[client-id]";
        options.ClientSecret = "[client-secret]";
    });
```
Replace the `[]` strings with the OAuth API URL, Client ID and Secret.

After `app.UseHttpsRedirection();` add `app.UseAuthentication();`:

```
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
```

## Rebuild and publish to console

Remove-Item .\publish\ -Recurse -Force; dotnet publish -c Debug --output=publish; Compress-Archive .\publish\* publish.zip -Force

In the Lambda Function console, click Upload from and choose .zip file
Click Upload and browse to the publish.zip file
Click Save

## Test to see 401 in Postman

Repeat the GET request in Postman
See response code 401 Unauthorized

## Acquire bearer token

Make POST request to your `{{ApiBaseUrl}}/oauth/token` endpoint to acquire JWT
Copy the `access_token` value
Add header: `Authorization:Bearer <token>` replacing `<token>` with the copied value

## Test to see 200 in Postman

Repeat the GET request in Postman
See resonse body contains "3"

# Additional dotnet CLI

List installed dotnet templates:
`dotnet new --list`

List installed dotnet templates filtered by tag 'aws':
`dotnet new --list --tag aws`

Install template library from nuget:
`dotnet new -i Amazon.Lambda.Templates`

Install dotnet lambda tools:
`dotnet tool install -g Amazon.Lambda.Tools`

Create project from template:
`dotnet new console -n MyConsoleProject`

Create minimal dotnet lambda project from template:
`dotnet new serverless.AspNetCoreMinimalAPI -n LambdaProject`

Publish deployable binary (ref):
`dotnet publish -c Release --output=publish`

Clean publish folder, build publish and zip:
```
Remove-Item .\publish\ -Recurse -Force
dotnet publish -c Debug --output=publish
Compress-Archive .\publish\* publish.zip -Force
```

Publish from command line:
`dotnet lambda deploy-function CleanCalculator -frun dotnet6`
