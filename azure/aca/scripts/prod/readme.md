# Steps
1. Multi-tenant service deployment 
    - 1.Mutlitenant FileAPI deployment    
2. Customer side setup
    - 2.Customer data source setup
    - 3.Customer compute n/w to upload data, delete customer compute
    - 4.Customer delegated network for injection
3. Platform Singletenant setup
    - 5.CreateSubnet for customer injection
    - 6.Create Env


## TODO
1. Create the Multiple api service serving through single tenant ACA
2. Image pull still through customer network - Resolve with Zhequan
3. Fix all the NSG and the right boundary
4. Add sample script to get token from Side car
5. Some subscriptions below are not enabled in Canary
    - 921496dc-987f-410f-bd57-426eb2611356
    - 47f1c914-e299-4953-a99d-3e34644cfe1c
    - ~6a6fff00-4464-4eab-a6b1-0b533c7202e0~ 
    - ~ea4faa5b-5e44-4236-91f6-5483d5b17d14~