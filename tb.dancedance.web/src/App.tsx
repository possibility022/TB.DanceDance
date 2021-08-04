import "./App.css"
import Player from "./components/Player"
import "tawian-frontend"
import "typeface-cousine"
import React from "react"
import { useDispatch } from "react-redux"
import {GetVideos} from "./actions/VideosActions"

class App extends React.Component {

	render(): JSX.Element{
		return(
			<Player></Player>
		)
	}
}

export default App
