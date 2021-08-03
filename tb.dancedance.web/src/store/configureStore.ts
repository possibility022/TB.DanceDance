import { configureStore } from "@reduxjs/toolkit"
import RootReducer from "../reduces/RootReducer"

const Store = configureStore({ reducer: RootReducer})

export type RootState = ReturnType<typeof RootReducer>
export default Store