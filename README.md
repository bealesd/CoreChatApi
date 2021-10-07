# CoreChatApi
## Running Locally
 1. Use dotnet user secrets for seting any app setting secrets
    1. dotnet user-secrets init
    2. dotnet user-secrets set "ConnectionStrings:password" "mySecret"
2. Use VSCode to run app locally
3. On push to main the app will auto deploy to azure at https://corechatapi.azurewebsites.net/
