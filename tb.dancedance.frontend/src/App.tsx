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
import { RequestAssignmentScreen } from "./pages/RequestAssignmentScreen"
import EventsScreen from "./pages/EventsScreen"
import { CookieModal } from "./components/CookieModal"
import { CookiesProvider } from "react-cookie"
import { AdministrateAccessRequestsScreen } from "./pages/AdministrateAccessRequestsScreen"

function App() {

	return (
		<section className="section">
			<CookiesProvider defaultSetOptions={{ path: '/' }}>
				<CookieModal></CookieModal>
				<AuthContext.Provider value={authService}>
					<BrowserRouter>
						<NavigationBar></NavigationBar>
						<Routes>
							<Route path="/" element={<Home />}>
							</Route>
							<Route path="videos/" element={<PrivateRoute element={<VideoScreen></VideoScreen>} />}>
							</Route>
							<Route path="videos/:videoId" element={<PrivateRoute element={<VideoPlayerScreen></VideoPlayerScreen>} />} />
							<Route path="videos/upload" element={<PrivateRoute element={<UploadVideo></UploadVideo>} />} />
							<Route path="callback" element={<Callback />} />
							<Route path="/videos/requestassignment" element={<RequestAssignmentScreen />} />
							<Route path="events" element={<PrivateRoute element={<EventsScreen></EventsScreen>} />} />
							<Route path="/access/requestedaccesses" element={<PrivateRoute element={<AdministrateAccessRequestsScreen></AdministrateAccessRequestsScreen>} />} />
							<Route path="logout" element={<Logout></Logout>} />
							<Route path="logout/callback" element={<LogoutCallback />} />
							{/* <Route path="/register" element={Register} /> */}
							<Route path="silentrenew" element={<SilentRenew />} />
							{/* <Route path="/" element={PublicPage} /> */}

						</Routes>
					</BrowserRouter>
				</AuthContext.Provider>
			</CookiesProvider>
		</section>
	)
}

export default App
