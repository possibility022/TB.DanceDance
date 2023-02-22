import NavigationBar from "./components/NavigationBar"
import "./App.css"
import "bulma/css/bulma.min.css"
import Home from "./pages/Home"

import { BrowserRouter, Routes, Route } from "react-router-dom"
import { Callback } from "./components/AuthComponents/Callback"
import { Logout } from "./components/AuthComponents/Logout"
import { LogoutCallback } from "./components/AuthComponents/LogoutCallback"
import { PrivateRoute } from "./components/AuthComponents/PrivateRoute"
import { SilentRenew } from "./components/AuthComponents/SilentRenew"
import { VideoScreen } from "./pages/VideosScreen"
import { VideoPlayerScreen } from "./pages/VideoPlayerScreen"
import { UploadVideo } from "./pages/UploadVideo"
import { AuthContext, authService } from "./providers/AuthProvider"

function App() {

	return (
		<div className="container">
			<AuthContext.Provider value={authService}>
				<BrowserRouter>
				<NavigationBar></NavigationBar>
					<Routes>
						<Route path="/" element={<Home />}>
						</Route>
						<Route path="videos/" element={<PrivateRoute element={<VideoScreen></VideoScreen>} />}>
						</Route>
						<Route path="videos/:videoId" element={<PrivateRoute element={<VideoPlayerScreen></VideoPlayerScreen>} />} />
						<Route path="video/upload" element={<PrivateRoute element={<UploadVideo></UploadVideo>} />} />
						<Route path="callback" element={<Callback />} />
						<Route path="logout" element={<Logout></Logout>} />
						<Route path="logout/callback" element={<LogoutCallback />} />
						{/* <Route path="/register" element={Register} /> */}
						<Route path="silentrenew" element={<SilentRenew />} />
						{/* <Route path="/" element={PublicPage} /> */}

					</Routes>
				</BrowserRouter>
			</AuthContext.Provider>
		</div>
	)
}

export default App
