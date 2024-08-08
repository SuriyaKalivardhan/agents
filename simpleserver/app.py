from fastapi import FastAPI, Request
from fastapi.responses import StreamingResponse
from typing import Any
import time
import psutil
import socket
from azure.identity import ManagedIdentityCredential
from azure.storage.blob import BlobServiceClient

app = FastAPI()

def isTrue(x:Any) -> bool:
    return str(x).lower() in ['true', 'yes', '1', 'y']

def streamecho(input:str, chunk_len:int):
    for chunk in range(chunk_len):
        time.sleep(0.25)
        yield f'echo {input} {chunk=}\n'

@app.get("/echo/{input}")
def echo(request:Request, input:str, chunks:int=10):
    s_h = request.headers.get("stream")
    stream = isTrue(s_h)
    if not stream:
        return f"Hello {input}"
    return StreamingResponse(streamecho(input, chunks), media_type='text/event-stream')

@app.get("/getips")
def getips():
    local_ips = []
    for interface, addrs in psutil.net_if_addrs().items():
        for addr in addrs:
            if addr.family == socket.AF_INET:
                local_ips.append((interface, addr.address))
    return local_ips

@app.get("/getembeddings/{client_id}/{account}/{container}/{blob}")
def getembedding(client_id:str, account:str, container:str, blob:str):
    try:
        creds = ManagedIdentityCredential(client_id=client_id)
        blob_service_client = BlobServiceClient(f"https://{account}.blob.core.windows.net/", creds)
        ctr_client = blob_service_client.get_container_client(container)
        blob_client = ctr_client.get_blob_client(blob)
        data = blob_client.download_blob()
        content = data.readall()
        content_str = content.decode("utf-8")
        return content_str
    except Exception as e:
        return f"{str(e)}"

@app.get("/gettoken/{client_id}")
def gettoken(client_id:str):
    try:
        creds = ManagedIdentityCredential(client_id=client_id)
        result = creds.get_token("https://management.azure.com")
        return result
    except Exception as e:
        return f"{str(e)}"