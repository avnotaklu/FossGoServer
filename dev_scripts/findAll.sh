db=baduk_server

if [[ $1 == "all" ]] then
	colls=$(mongosh --eval 'show collections' $db)
	mapfile -t arr <<< "$colls"

	for coll in "${arr[@]}"
	do
		echo -e "\e[31m$coll\e[0m"
		comm=$(printf "%s%s" "db." "$coll" ".find({})")
		res=$(mongosh --eval $comm $db)
		echo $res
	done
else
	comm=$(printf "%s%s" "db." "$1" ".find({})")
	res=$(mongosh --eval $comm $db)
	echo $res
fi
