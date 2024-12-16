import React from "react"
import { Container, createRoot } from 'react-dom/client';
import "./index.css"
import App from "./App"

// eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-call
const root = createRoot(document.getElementById("root") as Container)

// eslint-disable-next-line @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-unsafe-call
root.render(
	<React.StrictMode>
		<App />
	</React.StrictMode>
)
