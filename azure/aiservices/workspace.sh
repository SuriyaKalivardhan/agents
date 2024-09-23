#az login --use-device-code
location='westus3'
subscription='921496dc-987f-410f-bd57-426eb2611356'
resourcegroup='suriyak-ws-westus3'

if [ -z $1 ] || [ -z $2 ] || [ -z $3 ]; then
    echo "Using default location, subscription and resource group"
else
    location=$1
    subscription=$2
    resourcegroup=$3
fi

az configure -d location=$location subscription=$subscription group=$resourcegroup
echo location=$location subscription=$subscription resourcegroup=$resourcegroup
az account set -s $subscription
if [ $? != 0 ]; then
    exit 1
fi

username=$(whoami)
SkipAutoDeleteTill=$(date -d "+100 days" +"%Y-%m-%d")
az group create -n $resourcegroup --tags owner=$username SkipAutoDeleteTill=$SkipAutoDeleteTill
rgid=$(az group show -n $resourcegroup --query id -o tsv)

prefix=$(echo $resourcegroup | tr '-' '0')
hubprefix="hub"$prefix
hubuminame0=$hubprefix"umi0"
hubuminame1=$hubprefix"umi1"
hubuminame2=$hubprefix"umi2"
hubstoragename=$hubprefix"str"
hubkvname=$hubprefix"kv"
hubwsname=$hubprefix"ws0"

az identity create -n $hubuminame0
hubumi0id=$(az identity show -n $hubuminame0 --query id -o tsv)
hubumi0oid=$(az identity show -n $hubuminame0 --query principalId -o tsv)
hubumi0clientid=$(az identity show -n $hubuminame0 --query clientId -o tsv)

az identity create -n $hubuminame1
hubumi1id=$(az identity show -n $hubuminame1 --query id -o tsv)
hubumi1oid=$(az identity show -n $hubuminame1 --query principalId -o tsv)
hubumi1clientid=$(az identity show -n $hubuminame1 --query clientId -o tsv)

az storage account create -n $hubstoragename --allow-shared-key-access false
hubstorageid=$(az storage account show -n $hubstoragename --query id -o tsv)


az keyvault create -n $hubkvname
hubkvid=$(az keyvault show -n $hubkvname --query id -o tsv)

az role assignment create --role "Azure AI Administrator" --assignee-object-id $hubumi0oid --assignee-principal-type ServicePrincipal --scope $rgid

az group deployment create -n $hubwsname -f hubws.json --parameters location=$location workspaceName=$hubwsname umi0id=$hubumi0id umi1id=$hubumi1id storageAccountId=$hubstorageid keyVaultId=$hubkvid primaryUserAssignedIdentity=$hubumi0id
hubwsid=$(az ml workspace show -n $hubwsname --query id -o tsv)

projectprefix="project"$prefix
projectuminame0=$projectprefix"umi0"
projectuminame1=$projectprefix"umi1"
projectuminame2=$projectprefix"umi2"
projectwsname=$projectprefix"ws0"

az identity create -n $projectuminame0
projectumi0id=$(az identity show -n $projectuminame0 --query id -o tsv)
projectumi0oid=$(az identity show -n $projectuminame0 --query principalId -o tsv)
projectumi0clientid=$(az identity show -n $projectuminame0 --query clientId -o tsv)

az identity create -n $projectuminame1
projectumi1id=$(az identity show -n $projectuminame1 --query id -o tsv)
projectumi1oid=$(az identity show -n $projectuminame1 --query principalId -o tsv)
projectumi1clientid=$(az identity show -n $projectuminame1 --query clientId -o tsv)

az role assignment create --role "Azure AI Administrator" --assignee-object-id $projectumi1oid --assignee-principal-type ServicePrincipal --scope $rgid

az group deployment create -n $projectwsname -f projectws.json --parameters location=$location workspaceName=$projectwsname umi0id=$projectumi0id umi1id=$projectumi1id primaryUserAssignedIdentity=$projectumi1id hubResourceId=$hubwsid