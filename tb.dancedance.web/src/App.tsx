import "./App.css"
import Player from "./components/Player"
import "tawian-frontend"
import "typeface-cousine"
import React, { useEffect } from "react"
import { useDispatch, useSelector } from "react-redux"
import { GetSongs } from "./actions/SongActions"

class App extends React.Component {

	render(): JSX.Element{
		return(
			<Player></Player>
		)
	}
}

export default App
