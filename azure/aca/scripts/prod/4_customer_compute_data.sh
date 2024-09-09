#Replace the hostname and target resource/resourceid appropriately with the customer-setup.sh generated resources
location=eastus2euap
computepublicip=40.74.157.231
customerumi=/subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourcegroups/suriyak-customer2/providers/Microsoft.ManagedIdentity/userAssignedIdentities/suriyak0customer2-umi
customerumiclientid=2e4a2b18-67d9-4bfe-b8ef-4004b4f31500
customerstoragename=suriyak0customer2str
customeracasubnet=/subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourceGroups/suriyak-customer2/providers/Microsoft.Network/virtualNetworks/suriyak0customer2-vnet/subnets/suriyak0customer2-aca-subnet


customercontainer=embedctr
customerblob=embedblob
sub=$(cut -d'/' -f3 <<<$customerumi)
rg=$(cut -d'/' -f5 <<<$customerumi)

sudo apt-get update -y
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
sudo apt-get install tcptraceroute bc jq python3.10-venv -y
wget http://www.vdberg.org/~richard/tcpping
sudo cp tcpping /usr/bin
sudo chmod 755 /usr/bin/tcpping
#nslookup $customerstoragename.blob.core.windows.net
#tcpping $customerstoragename.blob.core.windows.net 80

az login --identity -u $customerumi
az account set -s $sub
az configure -d subscription=$sub group=$rg location=$location

echo "[0.54788036 0.52410722 0.27516717 0.56332735 0.99479976 0.32912522 0.17970737 0.77275429 0.16931099 0.27149482 0.55468555 0.06965549]" > /tmp/$customerblob.json
az storage container create --account-name $customerstoragename -n $customercontainer --auth-mode login
az storage blob upload -f /tmp/$customerblob.json --account-name $customerstoragename --container-name $customercontainer --auth-mode login -n $customerblob
access_token=$(az account get-access-token --resource https://$customerstoragename.blob.core.windows.net | jq -r .accessToken)
curl https://$customerstoragename.blob.core.windows.net/$customercontainer/$customerblob -H "Authorization: Bearer $access_token" -H "x-ms-version:2020-04-08"

#Below standlone app in temp compute
wget https://raw.githubusercontent.com/SuriyaKalivardhan/agents/main/simpleserver/app.py
wget https://raw.githubusercontent.com/SuriyaKalivardhan/agents/main/simpleserver/requirements.txt
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
uvicorn app:app --host 0.0.0.0 --port 8080