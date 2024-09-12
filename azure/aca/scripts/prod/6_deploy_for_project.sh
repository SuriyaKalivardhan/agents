#!/bin/bash
acaenvid=/subscriptions/921496dc-987f-410f-bd57-426eb2611356/resourceGroups/suriyak-customer3-chobov2/providers/Microsoft.App/managedEnvironments/suriyak-customer3-acaenv-4
customerumi=/subscriptions/ea4faa5b-5e44-4236-91f6-5483d5b17d14/resourcegroups/suriyak-customer3/providers/Microsoft.ManagedIdentity/userAssignedIdentities/suriyak0customer3-umi

location='eastus2euap'
commonHoBoSubscription='921496dc-987f-410f-bd57-426eb2611356'

name=$(cut -d'/' -f9 <<<$acaenvid)-4
az group deployment create -f forproject.json  --parameters name=$name identity=$customerumi envid=$acaenvid location=$location