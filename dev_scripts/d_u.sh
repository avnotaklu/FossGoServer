#!/usr/bin/bash

db=baduk_server

comm1=$(printf "%s%s" "db." "users" ".deleteOne({_id:ObjectId('$1')})")
comm2=$(printf "%s%s" "db." "users_ratings" ".deleteOne({_id:ObjectId('$1')})")
comm3=$(printf "%s%s" "db." "users_stats" ".deleteOne({_id:ObjectId('$1')})")

res=$(mongosh --eval $comm1 $db)
echo $comm1 $res

res=$(mongosh --eval $comm2 $db)
echo $comm2 $res

res=$(mongosh --eval $comm3 $db)
echo $comm3 $res
