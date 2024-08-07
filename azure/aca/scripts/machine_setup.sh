#nslookup suriyak0customerstr.blob.core.windows.net
curl -sL https://aka.ms/InstallAzureCLIDeb -y | sudo bash
sudo apt-get install tcptraceroute bc -y
wget http://www.vdberg.org/~richard/tcpping
sudo cp tcpping /usr/bin
sudo chmod 755 /usr/bin/tcpping
#nslookup suriyak0customerstr.blob.core.windows.net
#tcpping suriyak0customerstr.blob.core.windows.net 80
az login --identity -u /subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourcegroups/suriyak-customer/providers/Microsoft.ManagedIdentity/userAssignedIdentities/suriyak-customer-umi

az account set -s ea4faa5b-5e44-4236-91f6-5483d5b17d14
az configure -d subscription=ea4faa5b-5e44-4236-91f6-5483d5b17d14 group=suriyak-customer location=eastus2euap
az storage container create --account-name suriyak0customerstr -n embedctr --auth-mode login
#vi /tmp/embedblob.json
az storage blob upload -f /tmp/embedblob.json --account-name suriyak0customerstr --container-name embedctr --auth-mode login -n embedblob
az account get-access-token --resource https://suriyak0customerstr.blob.core.windows.net
curl https://suriyak0customerstr.blob.core.windows.net/embedctr/embedblob -H "Authorization: Bearer <jwt token>" -H "x-ms-version:2020-04-08"

#wget https://github.com/SuriyaKalivardhan/extnpoc/blob/main/SimpleServer/app.py
#pip install -r https://github.com/SuriyaKalivardhan/extnpoc/blob/main/requirements.txt
uvicorn app:app --host 0.0.0.0 --port 8080
curl http://20.252.250.72:8080/getembeddings/00c151b8-da3b-4209-8306-4d1b4e422c60/suriyak0customerstr/embedctr/embedblob