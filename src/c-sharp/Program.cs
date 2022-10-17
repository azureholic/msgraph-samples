using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http.Headers;

var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddUserSecrets<Program>()
            .Build();

//read config keys from secrets.json
//right-click your project in Visual Studio and choose "Manage User Secrets"
//this will prevent secrets to be commited in your code repository
var servicePrincipalClientId = config["clientId"];
var servicePrincipalClientSecret = config["clientSecret"];
var azureAdTenantId = config["tenantId"];
var azureAdContext = $"https://login.microsoftonline.com/{azureAdTenantId}";

//aquire a token for the MSGraph
//this will require your app registration to have access to Graph API scopes
//look at App Registrations in Azure AD -> API Permissions
//for these samples we will use Applications permissions, there is no user context
ClientCredential credentials = new ClientCredential(servicePrincipalClientId, servicePrincipalClientSecret);
var authenticationContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(azureAdContext);
AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync("https://graph.microsoft.com/", credentials);

//you can inspect the token in https://jwt.io to see its content
Console.WriteLine("------ We have aquired this token --------");
Console.WriteLine(authenticationResult.AccessToken);
Console.WriteLine("------------------------------------------");


//create an instance of the GraphSetviceClient
var graphServiceClient = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", authenticationResult.AccessToken);

                    return Task.CompletedTask;
                }));


//thse samples create everthing needed for a team call "GraphDemoTeam"
//API Permissions needed
//   Group.ReadWrite.All (or Group.Create)
//   EntitlementManagement.ReadWrite.All
//

var teamName = "GraphDemoTeam";
var groupName = teamName + "Group";
var catalogName = teamName + "Catalog";


//Create a Security Group
//create group info
var securityGroupInfo = new Group
{
    DisplayName = groupName,
    MailNickname = groupName.ToLower(),
    MailEnabled = false,
    SecurityEnabled = true,
    IsAssignableToRole = false
};

//create the group

//make sure the group does not exist
var groups = await graphServiceClient.Groups
    .Request()
    .Filter($"startswith(displayName, '{securityGroupInfo.DisplayName}')")
    .Top(1)
    .GetAsync();

Group securityGroup;
if (groups.Count == 0)
{
    securityGroup = await graphServiceClient.Groups
        .Request()
        .AddAsync(securityGroupInfo);
    Console.WriteLine($"created Group {securityGroup.DisplayName}");
}
else
{
    securityGroup = groups[0];
    Console.WriteLine($"Group {securityGroup.DisplayName} already exists");
}

//Create an AccessPackage Catalog
//define the catalog
var accessPackageCatalogInfo = new AccessPackageCatalog
{
    DisplayName = catalogName,
    IsExternallyVisible = false

};

//make sure the group does not exist
var accessPackageCatalogs = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackageCatalogs
    .Request()
    .Filter($"displayName eq '{accessPackageCatalogInfo.DisplayName}'")
    .Top(1)
    .GetAsync();

AccessPackageCatalog accessPackageCatalog;
if (accessPackageCatalogs.Count == 0)
{
    //create the catalog
    accessPackageCatalog = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackageCatalogs
    .Request()
    .AddAsync(accessPackageCatalogInfo);
    Console.WriteLine($"created Catalog {accessPackageCatalogInfo.DisplayName}");
}
else
{
    accessPackageCatalog = accessPackageCatalogs[0];
    Console.WriteLine($"Catalog {accessPackageCatalogInfo.DisplayName} already exists");
}

//Add the group to the catalog as a resource
var accessPackageResourceId = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackageCatalogs[accessPackageCatalog.Id].AccessPackageResources
    .Request()
    .Filter($"originId eq '{securityGroup.Id}'")
    .GetAsync();

if (accessPackageResourceId.Count == 0)
{
    //define the resource
    var accessPackageResourceRequestInfo = new AccessPackageResourceRequestObject
    {
        CatalogId = accessPackageCatalog.Id,
        RequestType = "AdminAdd",
        AccessPackageResource = new AccessPackageResource
        {
            OriginId = securityGroup.Id,
            OriginSystem = "AadGroup",
        }
    };

    //write the resource
    var accessPackageResourceRequest = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackageResourceRequests
                    .Request()
                    .AddAsync(accessPackageResourceRequestInfo);

    //retrieve the resourceId
    accessPackageResourceId = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackageCatalogs[accessPackageCatalog.Id].AccessPackageResources
        .Request()
        .Filter($"originId eq '{securityGroup.Id}'")
        .GetAsync();

    Console.WriteLine($"created {securityGroup.DisplayName} as a resource in catalog {accessPackageCatalogInfo.DisplayName}");

}
else
{
    Console.WriteLine($"{securityGroup.DisplayName} is already a resource in catalog {accessPackageCatalogInfo.DisplayName}");

}


//create an AccessPackage in the catalog
//define the accesspackage
var accessPackageInfo = new AccessPackage
{
    DisplayName = teamName,
    Description = "members of team " + teamName,
    IsHidden = false,
    CatalogId = accessPackageCatalog.Id
};

//make sure the AccessPackage does not exist
var accessPackages = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackages
    .Request()
    .Filter($"displayName eq '{accessPackageInfo.DisplayName}'")
    .Top(1)
    .GetAsync();

AccessPackage accessPackage;
if (accessPackages.Count == 0)
{

    //create the accesspackage
    accessPackage = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackages
   .Request()
   .AddAsync(accessPackageInfo);
    Console.WriteLine($"created AccessPackage {accessPackageInfo.DisplayName} in Catalog {accessPackageCatalog.DisplayName}");
}
else
{
    accessPackage = accessPackages[0];
    Console.WriteLine($"AccessPackage {accessPackageCatalogInfo.DisplayName} already exists");
}


//define a scope for the accesspackage
//Effect: When requested by a user, the user becomes a member of this group
var apResourceRoleScope = new AccessPackageResourceRoleScope
{
    AccessPackageResourceRole = new AccessPackageResourceRole
    {
        OriginId = "Member_" + securityGroup.Id,
        DisplayName = "Member",
        OriginSystem = "AadGroup",
        AccessPackageResource = new AccessPackageResource
        {
            Id = accessPackageResourceId[0].Id,
            ResourceType = "AadGroup",
            OriginId = securityGroup.Id,
            OriginSystem = "AadGroup"
        }
    },
    AccessPackageResourceScope = new AccessPackageResourceScope
    {
        OriginId = securityGroup.Id,
        OriginSystem = "AadGroup"
    }
};

//Add the scope to the accesspackage
string apId = accessPackage.Id;
var apRoleScope = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackages[accessPackage.Id].AccessPackageResourceRoleScopes
    .Request()
    .AddAsync(apResourceRoleScope);


//define a policy: who can request this and how is it approvaed
//in this case we will use automatic approval and all users can request
var accessPackageAssignmentPolicy = new AccessPackageAssignmentPolicy
{
    AccessPackageId = accessPackage.Id,
    DisplayName = "all users",
    Description = "all users can request",
    AccessReviewSettings = null,
    RequestorSettings = new RequestorSettings
    {
        ScopeType = "AllExistingDirectorySubjects",
        AcceptRequests = true,
        AllowedRequestors = new List<UserSet>()
        {
        }
    },
    RequestApprovalSettings = new ApprovalSettings
    {
        IsApprovalRequired = false,
        IsApprovalRequiredForExtension = false,
        IsRequestorJustificationRequired = false,
        ApprovalMode = "NoApproval",
        ApprovalStages = new List<ApprovalStage>()
        {
        }
    }
};

//add the policy to accesspackage
var apPolicy = await graphServiceClient.IdentityGovernance.EntitlementManagement.AccessPackageAssignmentPolicies
    .Request()
    .AddAsync(accessPackageAssignmentPolicy);

Console.WriteLine("Now goto https://myaccess.microsoft.com to request the access package");
Console.WriteLine("After requesting you should be a member of the securityGroup");



