import { combineReducers } from "redux"
import videoReducer from "./VideoReducer"
import playerReducer from "./PlayerReducer"

const RootReducer = combineReducers({
	video: videoReducer,
	player: playerReducer
})

export default RootReducer