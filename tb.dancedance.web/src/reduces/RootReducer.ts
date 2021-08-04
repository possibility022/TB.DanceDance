import { combineReducers } from "redux"
import videoReducer from "./VideoReducer"
import playerReducer from "./PlayerReducer"

const RootReducer = combineReducers({
	song: videoReducer,
	player: playerReducer
})

export default RootReducer