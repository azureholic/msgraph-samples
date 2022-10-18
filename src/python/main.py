import configparser
import aadgroupModel

from msgraph.core import GraphClient
from azure.identity import ClientSecretCredential

def main():
    print('Python Graph Sample')

    # Load settings
    config = configparser.ConfigParser()
    config.read('/secrets/python-msgraph.config')
    azure_settings = config['azure']
        
    # Build credentials and create GraphClient
    client_credential = ClientSecretCredential(azure_settings["tenantId"], azure_settings["clientId"], azure_settings["clientSecret"])
    graphServiceClient = GraphClient(credential=client_credential, scopes=['https://graph.microsoft.com/.default'])
    
    securtityGroup = aadgroupModel.Group()
    securtityGroup.DisplayName = "GraphDemoTeamGroup_Python"
    securtityGroup.MailNickname = "graphdemoteamgroup_python"
    securtityGroup.IsAssignableToRole = False
    securtityGroup.MailEnabled = False
    securtityGroup.SecurityEnabled = True

    #check if the group exists
    group_resp = graphServiceClient.get(f"/groups?$filter=displayName eq '{securtityGroup.DisplayName}'")
    
    if (len(group_resp.json()["value"]) == 0):
        group_resp = graphServiceClient.post("/groups", data=securtityGroup.Serialize(), headers={'Content-Type': 'application/json'})
        if (group_resp.ok) :
            print(f"Securitygroup {securtityGroup.DisplayName} created")
    else:
        print(f"A Securitygroup {securtityGroup.DisplayName} already exists")

    
    someusername = input('Enter a username (e.g. xyz@<yourdomain.onmicrosoft.com):')

    user_resp = graphServiceClient.get(f"/users?$filter=userPrincipalName eq '{someusername}'")
    if (len(user_resp.json()["value"]) != 0):
        #found the user, add user to group
        groupid = group_resp.json()["value"][0]["id"]
        userid = user_resp.json()["value"][0]["id"]

        memberInfo = aadgroupModel.Membership()
        memberInfo.id = f"https://graph.microsoft.com/v1.0/directoryObjects/{userid}"
        payload = memberInfo.Serialize()
        payload = payload.replace("id", "@odata.id")
     

        req_url = f"/groups/{groupid}/members/$ref"
        member_resp = graphServiceClient.post(req_url, data=payload, headers={'Content-Type': 'application/json'})
        print(member_resp)
    else:
        print("Username not found")

    

main()