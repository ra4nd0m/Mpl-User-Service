export type GetResult = {
    stream: NodeJS.ReadableStream;
    contentType: string;
    contentLength?: number;
};

export interface ObjectStore {
    put(key: string, body: NodeJS.ReadableStream, contentType: string): Promise<void>;
    get(key: string): Promise<GetResult>;
    del(key: string): Promise<void>;
}