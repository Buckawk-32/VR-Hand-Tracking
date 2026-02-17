var clientRef : StreamPeerTCP
var ID : String
var status : int  
var recevicedData : String
var sendData : PackedByteArray

signal emitData(newData: String)


func _init(currentClient: StreamPeerTCP) -> void:
	self.clientRef = currentClient
	print("Grabbed New Client!")
	storeID()


func storeID():
	self.clientRef.poll()
	recevicedData = decodeData()
	if recevicedData.contains("Confirmation"):
		ID = recevicedData.substr(11, -1)
		emitData.emit(ID)
		print("New Client Connected: " + ID)


func decodeData() -> String:
	var encodedData = clientRef.get_data(1024)
	if encodedData[0] == OK:
		recevicedData = bytes_to_var(encodedData[1])
		return recevicedData

	return "Error Encountered!"


func encodeData(data: Variant) -> PackedByteArray:
	sendData = var_to_bytes(data)
	return sendData
