import json

class Group:
    DisplayName = ""
    MailNickname = ""
    MailEnabled = False
    SecurityEnabled =  True
    IsAssignableToRole = False

    def Serialize(self):
        return json.dumps(self.__dict__)
		

class Membership:
    id = ""

    def Serialize(self):
        return json.dumps(self.__dict__)
		