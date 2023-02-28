

db.getCollection("users").drop();

var events = db.owners.find({"Name":{$exists:true}}).toArray()
db.createCollection("events")
db.events.insertMany(events)

var groups = db.owners.find({"GroupName":{$exists: true}}).toArray()
db.createCollection("groups")
db.groups.insertMany(groups)

db.getCollection("owners").drop()

db.owners.updateMany(
{},
{$set:{"EventType": 3}}
)