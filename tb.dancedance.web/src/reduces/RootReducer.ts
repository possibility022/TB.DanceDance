import { combineReducers } from "redux"
import songReducer from "./SongReducer"
import playerReducer from "./PlayerReducer"

const RootReducer = combineReducers({
	song: songReducer,
	player: playerReducer
})

export default RootReducer