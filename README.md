# msgraph-samples

This sample shows how to use the MS Graph in IAM scenario's. The C# program will demo how to create an Access Package that, when requested by a user, will make that user member of a security group.
Access Requests can be done by users in the [My Access Portal for your tenant](https://myaccess.microsoft.com)

More samples for different scenario's will follow.

### Prerequisites

You will have to have permissions on your Azure AD to be able to Grant Admin Consent for API Access.

Create an app registration in your Azure AD and generate a Secret (make sure to copy the secret as it will only be showed once).

On the API Permissions blade, add Application Permissions for the MS Graph for these scopes

* EntitlementManagement.ReadWrite.All
* Group.ReadWrite.All

Make sure you Grant Admin Consent for these permissions after adding them.

After cloning the repo and opening the C# solution in Visual Studio, right-click the project and choose Manage User Secrets.

Transfer the values from the Client Registration

* Application (client) ID
* Directory (tenant) ID
* and the Secret you've created in a previous step.

Enter the keys and values in the secret.json in Visual Studio. This will prevent commiting this data to your Git Repository.

```
{ 
  "clientId" : "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", `
  "clientSecret":"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "tenantId" : "xxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx" `
}
```


This sample uses the beta-version of the MSGraph SDK for C#