import httpApiClient from "./HttpApiClient";
import {CommentResponse} from "../types/ApiModels/Comments/CommentResponse";
import CreateCommentRequest from "../types/ApiModels/Comments/AddCommentRequest";
import appStorage from "../providers/AppStorage";
import {AxiosRequestConfig} from "axios";

class CommentsService {

    private getConfigWithAnonymouseHeader(): AxiosRequestConfig<any> {
        return {
            headers: {
                "AnonymousId": appStorage.getAnonymousId()
            }
        }
    }

    async getCommentsByLink(linkId: string) {
        const response = await httpApiClient.get<CommentResponse[]>(
            `/api/share/${linkId}/comments`,
            this.getConfigWithAnonymouseHeader())
        return response.data
    }

    async getCommentsByVideoId(videoBlobId: string) {
        const response = await httpApiClient.get<CommentResponse[]>(`/api/comments/video/${videoBlobId}`)
        return response.data
    }

    async addCommentAsync(linkId: string, comment: string) {
        const request: CreateCommentRequest = {
            content: comment
        }
        await httpApiClient.post(`/api/share/${linkId}/comments`, request)
    }

    async addCommentAsAnonymouseAsync(linkId: string, comment: string, authorName: string) {
        const request: CreateCommentRequest = {
            content: comment,
            authorName: authorName,
            anonymouseId: appStorage.getAnonymousId()
        }

        await httpApiClient.post(`/api/share/${linkId}/comments`, request)
    }
}

const commentsService = new CommentsService();
export default commentsService;