#!/usr/bin/bash

db=baduk_server

if [[ $1 == "all" ]] then
	colls=$(mongosh --eval 'show collections' $db)
	mapfile -t arr <<< "$colls"

	for coll in "${arr[@]}"
	do
		comm=$(printf "%s%s" "db." "$coll" ".deleteMany({})")
		res=$(mongosh --eval $comm $db)
		echo $comm $res
	done
else
	comm=$(printf "%s%s" "db." "$1" ".deleteMany({})")
	res=$(mongosh --eval $comm $db)
	echo $comm $res
fi
