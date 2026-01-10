import httpApiClient from "./HttpApiClient";
import {CommentResponse} from "../types/ApiModels/Comments/CommentResponse";
import CreateCommentRequest from "../types/ApiModels/Comments/AddCommentRequest";

class CommentsService {
    async getCommentsByLink(linkId: string) {
        const response = await httpApiClient.get<CommentResponse[]>(`/api/share/${linkId}/comments`)
        return response.data
    }

    async getCommentsByVideoId(videoBlobId: string) {
        const response = await httpApiClient.get<CommentResponse[]>(`/api/comments/video/${videoBlobId}`)
        return response.data
    }

    async addCommentAsync(linkId: string, comment: string, authorName?: string) {
        const request: CreateCommentRequest = {
            content: comment,
            authorName: authorName
        }
        await httpApiClient.post(`/api/share/${linkId}/comments`, request)
    }
}

const commentsService = new CommentsService();
export default commentsService;