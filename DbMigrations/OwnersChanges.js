

db.getCollection("users").drop();

var events = db.owners.find({"Name":{$exists:true}}).toArray()
db.createCollection("events")
db.events.insertMany(events)

var groups = db.owners.find({"GroupName":{$exists: true}}).toArray()
db.createCollection("groups")
db.groups.insertMany(groups)

db.getCollection("owners").drop()

db.events.updateMany(
{},
{$set:{"EventType": 3}}
)

db.groups.updateMany({},
{$set: {"People": []} }
)

db.events.updateMany({},
{$set: {"Attenders": []}}
)

db.createCollection("requestedAssignment")