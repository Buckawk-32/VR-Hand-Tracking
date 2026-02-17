extends Node3D
	

@export var host : String = "127.0.0.1"
@export var port : int = 4001

var Client = load("res://Scripts/Client.gd")

var addr : Array = [host, port]
var server : TCPServer = TCPServer.new()

var client : Variant
var clients : Array

func _ready() -> void:
	if server.listen(addr[1], addr[0]) == OK:
		print("Server Started on port: " + str(server.get_local_port()))
	else:
		print("ERROR: Server Failed to start on port: " + addr[1])
		return

func _process(delta: float) -> void:
	grabClient()	
	#processClients()



func grabClient() -> void:
	if server.is_connection_available():
		client = Client.new(server.take_connection())
		await client.emitData()
		clients.append(client)

		return
	

#func processClients() -> void:
	#if clients.size() != 0:
#
	#return
